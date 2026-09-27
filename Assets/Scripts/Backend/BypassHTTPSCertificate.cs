// Autor: Murillo Gomes Yonamine | Professor Eduardo
// Data: 27/09/2026

using UnityEngine.Networking;

public class BypassHTTPSCertificate : CertificateHandler
{
    /*
        A Unity, por padrão, só suporta servidores com segurança HTTPS. Caso seu
        servidor não tenha SSL instalado (como o servidor de desenvolvimento XAMPP),
        para driblar essa segurança, podemos utilizar o código abaixo:
    */
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}