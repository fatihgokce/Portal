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
    
    public partial class Tema
    {
        public int temaID { get; set; }
        public string temaAdresi { get; set; }
        public string temaDemoAdi { get; set; }
        public string temaFirmaAdi { get; set; }
        public Nullable<int> temaDomainKategoriID { get; set; }
    
        public virtual DomainKategori DomainKategori { get; set; }
    }
}