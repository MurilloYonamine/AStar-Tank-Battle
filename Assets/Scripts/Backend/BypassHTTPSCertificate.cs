// Autor: Murillo Gomes Yonamine | Professor Eduardo
// Data: 27/09/2026

using UnityEngine.Networking;

/// <summary>
/// Legacy classroom certificate override, retained only by the old public player-list request.
/// It accepts every HTTPS certificate and therefore disables certificate verification; it is
/// not needed to turn HTTP into HTTPS and must not be treated as production transport security.
/// The active login, registration, match-save and time-ranking requests do not attach it.
/// </summary>
public class BypassHTTPSCertificate : CertificateHandler
{
    // Returning true is unconditional acceptance, not validation of certificate identity.
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}
