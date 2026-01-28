using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ONERI.Models
{
    public class VeriYukleViewModel
    {
        public List<IFormFile> Dosyalar { get; set; } = new List<IFormFile>();

        public List<VeriYukleResult> Results { get; set; } = new List<VeriYukleResult>();
    }

    public class VeriYukleResult
    {
        public string DosyaAdi { get; set; } = "";
        public string SayfaAdi { get; set; } = "";
        public int? SatirSayisi { get; set; }
        public string Mesaj { get; set; } = "";
    }
}
