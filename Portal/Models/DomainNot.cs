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
    
    public partial class DomainNot
    {
        public int DomainNotId { get; set; }
        public string DomainNotNot { get; set; }
        public Nullable<System.DateTime> DomainNotTarih { get; set; }
        public string RefNotKullaniciId { get; set; }
    
        public virtual AspNetUser AspNetUser { get; set; }
    }
}