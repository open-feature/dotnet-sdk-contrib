using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace OpenFeature.Contrib.Providers.Flagd.Utils;

internal static class CertificateLoader
{
    internal static X509Certificate2 LoadCertificate(string certificatePath)
    {
        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            return null;
        }

        if (!File.Exists(certificatePath))
        {
            throw new FileNotFoundException($"Certificate file not found: {certificatePath}");
        }

#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificateFromFile(certificatePath);
#else
        return new X509Certificate2(certificatePath);
#endif
    }
}
