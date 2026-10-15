namespace BidArena.Infrastructure.Services;

/// <summary>
/// Podešavanja Cloudflare R2 skladišta (S3-kompatibilno). Koristi se kad je FileStorage:Provider = "R2".
/// </summary>
public class R2StorageSettings
{
    // npr. https://ACCOUNT_ID.r2.cloudflarestorage.com
    public string ServiceUrl { get; set; } = "";

    public string Bucket { get; set; } = "";

    public string AccessKeyId { get; set; } = "";

    public string SecretAccessKey { get; set; } = "";

    // Javni URL bucket-a (r2.dev poddomen ili sopstveni domen), bez završne kose crte.
    // Vraća se klijentu kao URL slike, npr. https://pub-xxxx.r2.dev
    public string PublicBaseUrl { get; set; } = "";

    // Region za potpisivanje zahteva. Prazno = podrazumevano (R2). Supabase Storage traži region projekta, npr. eu-west-1.
    public string Region { get; set; } = "";

    // R2 prima nepotpisan sadržaj (true). Supabase Storage traži potpisan sadržaj, pa tamo postavi false.
    public bool DisablePayloadSigning { get; set; } = true;
}
