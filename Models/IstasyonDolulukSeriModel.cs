namespace ONERI.Models;

public class IstasyonDolulukSeriModel
{
    public string Bolum { get; set; } = string.Empty;
    public List<double> DolulukOranlari { get; set; } = new();
    public List<int> ModulSayilari { get; set; } = new();
}
