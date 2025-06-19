using System;
using System.IO;
using System.Text.Json;
using OpenFeature.Constant;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v1.converters;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.exception;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.model;
using OpenFeature.Contrib.Providers.GOFeatureFlag.v2.wasm.bean;
using Wasmtime;

namespace OpenFeature.Contrib.Providers.GOFeatureFlag.v2.wasm;

/// <summary>
///     EvaluationWasm is a class that represents the evaluation of a feature flag
///     it calls an external WASM module to evaluate the feature flag.
/// </summary>
public class EvaluateWasm
{
    /// Function to evaluate the feature flag in the WASM module.
    private readonly Function _wasmEvaluate;

    /// Function to free memory in the WASM module.
    private readonly Function _wasmFree;

    /// Function to allocate memory in the WASM module.
    private readonly Function _wasmMalloc;

    /// Function to get access to the WASM memory.
    private readonly Memory _wasmMemory;

    /// <summary>
    ///     Constructor of the EvaluationWasm. It initializes the WASM module and the host functions.
    /// </summary>
    public EvaluateWasm()
    {
        // TODO: change the path of the wasm file
        var wasmFilePath =
            "/Users/thomas.poignant/dev/thomaspoignant/go-feature-flag/out/bin/gofeatureflag-evaluation.wasi";
        if (!File.Exists(wasmFilePath))
        {
            throw new FileNotFoundException($"Wasm file not found at '{wasmFilePath}'. Please ensure the file exists.");
        }

        var engine = new Engine();
        var linker = new Linker(engine);
        var store = new Store(engine);

        var wasmBytes = File.ReadAllBytes(wasmFilePath);
        var module = Module.FromBytes(engine, "evaluation", wasmBytes);
        var wasi = new WasiConfiguration()
            .WithInheritedStandardOutput()
            .WithInheritedStandardError();
        linker.DefineWasi();
        store.SetWasiConfiguration(wasi);
        var instance = linker.Instantiate(store, module);
        this._wasmMemory = instance.GetMemory("memory") ?? throw new WasmFunctionNotFound("memory");
        this._wasmMalloc = instance.GetFunction("malloc") ?? throw new WasmFunctionNotFound("malloc");
        this._wasmFree = instance.GetFunction("free") ?? throw new WasmFunctionNotFound("free");
        this._wasmEvaluate = instance.GetFunction("evaluate") ?? throw new WasmFunctionNotFound("evaluate");
    }

    /// <summary>
    ///     Evaluates a feature flag using the WASM module.
    /// </summary>
    /// <typeparam name="T">Expected type of the feature flag</typeparam>
    /// <returns>A ResolutionDetails of the feature flag</returns>
    /// <exception cref="WasmInvalidResult">If for any reasons we have an issue calling the wasm module.</exception>
    public EvaluationResponse Evaluate(WasmInput wasmInput)
    {
        var wasmInputAsStr = JsonSerializer.Serialize(wasmInput, JsonConverterExtensions.DefaultSerializerSettings);
        var inputPtr = this.CopyToMemory(wasmInputAsStr);
        try
        {
            var evaluateRes = this._wasmEvaluate.Invoke(inputPtr, wasmInputAsStr.Length) as long?
                              ?? throw new WasmInvalidResult("Evaluate should return a long value.");
            var resAsString = this.ReadFromMemory(evaluateRes);
            var goffResp = JsonSerializer.Deserialize<EvaluationResponse>(resAsString);
            return goffResp;
        }
        catch (Exception ex)
        {
            return new EvaluationResponse
            {
                ErrorCode = nameof(ErrorType.General), Reason = Reason.Error, ErrorDetails = ex.Message
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
    /// <exception cref="WasmInvalidResult">If for any reasons we have an issue calling the wasm module.</exception>
    private int CopyToMemory(string inputString)
    {
        // Allocate memory in the Wasm module for the input string.
        var ptr = this._wasmMalloc.Invoke(inputString.Length + 1) as int?
                  ?? throw new WasmInvalidResult("Malloc should return an int value.");
        this._wasmMemory.WriteString(ptr, inputString);
        return ptr;
    }

    /// <summary>
    ///     Reads the output string from the WASM memory based on the result of the evaluation.
    /// </summary>
    /// <param name="evaluateRes">result of the evaluate function</param>
    /// <returns>A string containing the output of the evaluate function</returns>
    /// <exception cref="WasmInvalidResult">If for any reasons we have an issue calling the wasm module.</exception>
    private string ReadFromMemory(long evaluateRes)
    {
        var ptr = (int)(evaluateRes >> 32); // Higher 32 bits for a pointer
        var outputStringLength = (int)(evaluateRes & 0xFFFFFFFF); // Lower 32 bits for length
        if (ptr == 0 || outputStringLength == 0)
        {
            throw new WasmInvalidResult("Output string pointer or length is invalid.");
        }

        var result = this._wasmMemory.ReadString(ptr, outputStringLength);
        return result;
    }
}
