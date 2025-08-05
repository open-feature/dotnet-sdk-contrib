using System;
using System.Reflection;
using System.Text.Json;
using OpenFeature.Constant;
using OpenFeature.Providers.GOFeatureFlag.Converters;
using OpenFeature.Providers.GOFeatureFlag.Exceptions;
using OpenFeature.Providers.GOFeatureFlag.Models;
using OpenFeature.Providers.GOFeatureFlag.Wasm.Bean;
using Wasmtime;
using Module = Wasmtime.Module;

namespace OpenFeature.Providers.GOFeatureFlag.Wasm;

/// <summary>
///     EvaluationWasm is a class that represents the evaluation of a feature flag
///     it calls an external WASM module to evaluate the feature flag.
/// </summary>
public class EvaluateWasm : IDisposable
{
    private const string ResourceName =
        "OpenFeature.Providers.GOFeatureFlag.WasmModules.gofeatureflag-evaluation.wasi";

    /// Function to evaluate the feature flag in the WASM module.
    private readonly Function _wasmEvaluate;

    /// Function to free memory in the WASM module.
    private readonly Function _wasmFree;

    /// Function to allocate memory in the WASM module.
    private readonly Function _wasmMalloc;

    /// Function to get access to the WASM memory.
    private readonly Wasmtime.Memory _wasmMemory;

    /// <summary>
    ///     Engine instance for WASM execution.
    /// </summary>
    private readonly Engine _engine;

    /// <summary>
    ///     Store instance for WASM state management.
    /// </summary>
    private readonly Store _store;

    /// <summary>
    ///     Flag to track if the object has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    ///     Constructor of the EvaluationWasm. It initializes the WASM module and the host functions.
    /// </summary>
    public EvaluateWasm()
    {
        this._engine = new Engine();
        var linker = new Linker(this._engine);
        this._store = new Store(this._engine);

        var assembly = Assembly.GetExecutingAssembly();
        if (assembly == null)
        {
            throw new WasmNotLoadedException("Assembly not found. Ensure the WASM module is correctly embedded.");
        }

        var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream == null)
        {
            throw new WasmNotLoadedException(
                "WASM module not found. Ensure the resource name is correct and the file is included in the project.");
        }

        var module = Module.FromStream(this._engine, "evaluation", stream);
        var wasi = new WasiConfiguration()
            .WithInheritedStandardOutput()
            .WithInheritedStandardError();
        linker.DefineWasi();
        this._store.SetWasiConfiguration(wasi);
        var instance = linker.Instantiate(this._store, module);
        this._wasmMemory = instance.GetMemory("memory") ?? throw new WasmFunctionNotFoundException("memory");
        this._wasmMalloc = instance.GetFunction("malloc") ?? throw new WasmFunctionNotFoundException("malloc");
        this._wasmFree = instance.GetFunction("free") ?? throw new WasmFunctionNotFoundException("free");
        this._wasmEvaluate = instance.GetFunction("evaluate") ?? throw new WasmFunctionNotFoundException("evaluate");
    }

    /// <summary>
    ///     Evaluates a feature flag using the WASM module.
    /// </summary>
    /// <returns>A ResolutionDetails of the feature flag</returns>
    /// <exception cref="WasmInvalidResultException">If for any reasons we have an issue calling the wasm module.</exception>
    public EvaluationResponse Evaluate(WasmInput wasmInput)
    {
        if (this._disposed)
        {
            throw new ObjectDisposedException(nameof(EvaluateWasm));
        }

        var wasmInputAsStr = JsonSerializer.Serialize(wasmInput, JsonConverterExtensions.DefaultSerializerSettings);
        var inputPtr = this.CopyToMemory(wasmInputAsStr);
        try
        {
            var evaluateRes = this._wasmEvaluate.Invoke(inputPtr, wasmInputAsStr.Length) as long?
                              ?? throw new WasmInvalidResultException("Evaluate should return a long value.");
            var resAsString = this.ReadFromMemory(evaluateRes);
            var goffResp = JsonSerializer.Deserialize<EvaluationResponse>(resAsString);
            if (goffResp == null)
            {
                throw new WasmInvalidResultException("Deserialization of EvaluationResponse failed.");
            }
            return goffResp;
        }
        catch (Exception ex)
        {
            return new EvaluationResponse
            {
                ErrorCode = nameof(ErrorType.General),
                Reason = Reason.Error,
                ErrorDetails = ex.Message
            };
        }
        finally
        {
            if (inputPtr != 0)
            {
                this._wasmFree.Invoke(inputPtr);
            }
        }
    }

    /// <summary>
    ///     Copies the input string to the WASM memory and returns the pointer to the memory location.
    /// </summary>
    /// <param name="inputString">string to put in memory</param>
    /// <returns>the address location of this string</returns>
    /// <exception cref="WasmInvalidResultException">If for any reasons we have an issue calling the wasm module.</exception>
    private int CopyToMemory(string inputString)
    {
        // Allocate memory in the Wasm module for the input string.
        var ptr = this._wasmMalloc.Invoke(inputString.Length + 1) as int?
                  ?? throw new WasmInvalidResultException("Malloc should return an int value.");
        this._wasmMemory.WriteString(ptr, inputString);
        return ptr;
    }

    /// <summary>
    ///     Reads the output string from the WASM memory based on the result of the evaluation.
    /// </summary>
    /// <param name="evaluateRes">result of the evaluate function</param>
    /// <returns>A string containing the output of the evaluate function</returns>
    /// <exception cref="WasmInvalidResultException">If for any reasons we have an issue calling the wasm module.</exception>
    private string ReadFromMemory(long evaluateRes)
    {
        var ptr = (int)(evaluateRes >> 32); // Higher 32 bits for a pointer
        var outputStringLength = (int)(evaluateRes & 0xFFFFFFFF); // Lower 32 bits for length
        if (ptr == 0 || outputStringLength == 0)
        {
            throw new WasmInvalidResultException("Output string pointer or length is invalid.");
        }

        var result = this._wasmMemory.ReadString(ptr, outputStringLength);
        return result;
    }

    /// <summary>
    ///     Disposes of the WASM resources.
    /// </summary>
    public void Dispose()
    {
        if (!this._disposed)
        {
            // In Wasmtime 22.0.0, most objects are automatically managed
            // The Engine and Store are the main resources that need cleanup
            // Other objects like Instance, Module, Linker are managed by the Store/Engine
            this._store?.Dispose();
            this._engine?.Dispose();

            this._disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
