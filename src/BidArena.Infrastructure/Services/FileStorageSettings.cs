namespace BidArena.Infrastructure.Services;

public class FileStorageSettings
{
    // Folder na disku (relativan se razresava u odnosu na radni direktorijum aplikacije).
    public string RootPath { get; set; } = "uploads";

    // URL prefiks pod kojim se fajlovi serviraju (UseStaticFiles).
    public string PublicPath { get; set; } = "/uploads";
}
