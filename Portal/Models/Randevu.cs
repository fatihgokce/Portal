//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Portal.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Randevu
    {
        public int RandevuID { get; set; }
        public Nullable<int> RandevuKonumID { get; set; }
        public Nullable<System.DateTime> RandevuTarihi { get; set; }
        public Nullable<int> RandevuRefArayanID { get; set; }
        public Nullable<int> RandevuRefFirmaID { get; set; }
        public string RandevuDetay { get; set; }
        public string RandevuEkleyenID { get; set; }
        public string RandevuYetkiliKisiID { get; set; }
        public Nullable<System.DateTime> RandevuKayitTarihi { get; set; }
        public string RandevuMusteriTipi { get; set; }
        public Nullable<int> RandevuSiraNo { get; set; }
        public Nullable<bool> RandevuSilDurum { get; set; }
        public string RandevuYeri { get; set; }
    
        public virtual Konum Konum { get; set; }
        public virtual Firma Firma { get; set; }
        public virtual Arayanlar Arayanlar { get; set; }
    }
}
