﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Portal.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using Portal.Models.IslerModels;
using System.Data.Entity;
namespace Portal.Controllers
{
    public class IslerController : BaseController
    {
       
        // GET: Isler
        public ActionResult Index(bool kontrolBekleyenIsler, bool onaylananIsler, int? RefBolgeID, int? SayfaNo)
        {
            ViewBag.SayfaSiraNo = SayfaNo ?? 1;
            ViewBag.kontrolBekleyenIsler = kontrolBekleyenIsler;
            ViewBag.onaylananIsler = onaylananIsler;
            ViewBag.RefBolgeID = RefBolgeID;

            int GosterilecekEleman = (SayfaNo ?? 1) * 10;
            int baslanacakSira = GosterilecekEleman - 10;
            using (var db = new PortalEntities())
            {
                string kullaniciID = User.Identity.GetUserId();
                IEnumerable<isler> islerim = db.islers.GetirIsler(kontrolBekleyenIsler, onaylananIsler, kullaniciID, RefBolgeID).Skip(baslanacakSira).Take(10).ToList();
                ViewBag.ToplamEleman = islerim.ToList().Count;

                return View(islerim);
            }

        }

        // Summary:
        //     Domain e ait is bilgileri goserilen sayfa
        // Parameters:
        //   id:domain id
        //  
        #region domain isler   
        public ActionResult DomainIsler(int? id)
        {
            id = id ?? 13448;
            ViewBag.domainId = id;
            var domain = Db.Domains.SingleOrDefault(x=>x.DomainID==id);
            ViewBag.kullanicilar = Db.AspNetUsers.Where(x => x.LockoutEnabled == false).ToList() ;
            //todo:login ekrani olunca değiştirilecek
            //User.Identity.IsAuthenticated ? 
            string userId= User.Identity.GetUserId() ?? "f5f53da2-c311-44c9-af6a-b15ca29aee57";
            ViewBag.guncelKullanici = Db.AspNetUsers.Where(x => x.Id == userId).
                                      Select(x => new Kullanici { Id = x.Id, AdSoyad = x.Isim + " " + x.SoyIsim }).FirstOrDefault();

            return View(domain);
        }       
        public JsonResult DomainAitIsler(int domainId)
        {
            var list = (from p in Db.islers.Include(x => x.IsiYapacakKisis).Include(x=>x.isYorums)
                        join a in Db.AspNetUsers on p.islerisiVerenKisi equals a.Id
                        join z in Db.ZamanIs on p.islerID equals z.RefIsId into temp
                        from zz in temp.DefaultIfEmpty()
                        where p.islerRefDomainID == domainId 
                        orderby p.islerSiraNo descending
                        select new DomainIs
                        {
                            IsId = p.islerID,
                            IsAd = p.islerAdi,
                            IsAciklama = p.islerAciklama,
                            FirmaId = p.islerRefFirmaID.Value,
                            IsiVerenKullanici=new Kullanici { Id=p.islerisiVerenKisi,AdSoyad=a.Isim+" "+a.SoyIsim},
                            IsDurum=p.islerIsinDurumu,
                            SiraNo=p.islerSiraNo ?? 1,
                            BitisTarihiVarmi=p.islerBitisTarihiVarmi,
                            BitisTarihi=p.islerBitisTarihi,
                            Tarih=p.islerTarih,
                            Yorumlar=(from y in p.isYorums
                                      join q in Db.AspNetUsers on y.isYorumRefYorumuYapanID equals q.Id
                                      orderby y.isYorumKayitTarih ascending
                                      select new YorumIs
                                      { KullaniciId=q.Id,Aciklama=y.isYorumAciklama,Tarih=y.isYorumKayitTarih.Value,AdSoyad=q.Isim+" "+q.SoyIsim}
                                      ).ToList(),
                            IsGecenZaman=new GecenZaman { GecenZamanSaniye=zz.GecenZamanSaniye,ZamanBasTarih=zz.ZamanIsBasTarih},
                            
                            IsiYapacakKullanicilar = (from pf in p.IsiYapacakKisis
                                                      join q in Db.AspNetUsers on pf.RefIsiYapacakKisiUserID equals q.Id
                                                      where pf.RefIsID == p.islerID
                                                      select new Kullanici {
                                                          Id = pf.RefIsiYapacakKisiUserID,AdSoyad=q.Isim+" "+q.SoyIsim
                                                      }
                                                    ).ToList()
                        }
                        
                        );               
            
            return Json(list,JsonRequestBehavior.AllowGet);
        }
        [ValidateInput(false)]
        public JsonResult IsDurumuDegistir(string domainIs,byte yeniDurum)
        {
            IsinDurumu yeniisDurum = (IsinDurumu) yeniDurum;
            var microsoftDateFormatSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Local
            };
            DomainIs obj = JsonConvert.DeserializeObject<DomainIs>(domainIs, microsoftDateFormatSettings);
            JsonCevap jsn = new JsonCevap();
            jsn.Basarilimi = true;
            try
            {
                ZamanI zamanIs = Db.ZamanIs.SingleOrDefault(x => x.RefIsId == obj.IsId);
                if(zamanIs!=null)
                {
                    if (yeniisDurum == IsinDurumu.Yapiliyor)
                    {                       
                        zamanIs.ZamanIsBasTarih = DateTime.Now;                        
                        obj.IsGecenZaman.ZamanBasTarih = DateTime.Now;                      
                    }
                    else
                    {
                        //islerIsinDurumu Yapilacak veya YapilacakDeadline
                        var diffInSeconds = (DateTime.Now - obj.IsGecenZaman.ZamanBasTarih.Value).TotalSeconds;
                        zamanIs.GecenZamanSaniye = zamanIs.GecenZamanSaniye + (long)diffInSeconds;
                        zamanIs.ZamanIsBasTarih = DateTime.Now;
                        obj.IsGecenZaman.ZamanBasTarih = DateTime.Now;
                        obj.IsGecenZaman.GecenZamanSaniye = zamanIs.GecenZamanSaniye;
                    }
                }else
                {
                    //zamanis kayit yok ise islerIsinDurumu Yapilacak veya YapilacakDeadline dir
                    zamanIs = new ZamanI() { GecenZamanSaniye = 0, RefIsId = obj.IsId, ZamanIsBasTarih = DateTime.Now };
                    Db.ZamanIs.Add(zamanIs);
                    obj.IsGecenZaman.ZamanBasTarih = DateTime.Now;
                    obj.IsGecenZaman.GecenZamanSaniye = 0;

                }
                isler job = Db.islers.SingleOrDefault(x => x.islerID == obj.IsId);
                job.islerIsinDurumu = (int)yeniisDurum;
                obj.IsDurum= (int)yeniisDurum;
                jsn.Data = obj;
                Db.SaveChanges();
            }
            catch
            {
                jsn.Basarilimi = false;
            }
          
            return Json(jsn,JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetirDomainNotlari(int domainId)
        {
            var list = (from dn in Db.DomainNots
                        join d in Db.Domains on dn.RefDomainId equals d.DomainID
                        join f in Db.Firmas on d.RefDomainFirmaID equals f.FirmaID
                        join u in Db.AspNetUsers on dn.RefNotKullaniciId equals u.Id
                        orderby dn.DomainNotTarih descending
                        where dn.RefDomainId==domainId
                        select new
                        {
                            Id = dn.DomainNotId,
                            Note = dn.DomainNotNot,
                            DomainAdi = d.DomainAdi,
                            DomainId = d.DomainID,
                            FirmaAdi = f.FirmaAdi,
                            AdSoyad = u.Isim + " " + u.SoyIsim,
                            Tarih = dn.DomainNotTarih
                        }
                     ).ToList();
            return Json(list,JsonRequestBehavior.AllowGet);

        }
        public JsonResult FirmaKisiler(int firmaId)
        {
            var list = (from fl in Db.FirmaKisis.Include(x => x.Firma)
                        orderby fl.Firma.FirmaAdi descending                      
                        select new
                        {
                            Id = fl.Id,
                            Ad = fl.Ad,
                            Soyad = fl.Soyad,
                            Departman = fl.Departman,
                            Tel = fl.Tel,
                            Email = fl.Email,
                            FirmaAdi = fl.Firma.FirmaAdi
                        }
                       ); 
            return Json(list.ToList(), JsonRequestBehavior.AllowGet);
        }
        public JsonResult YorumKaydet(string kullaniciId,string aciklama,int isId)
        {
            JsonCevap jsn = new JsonCevap();

            jsn.Basarilimi = true;
            isYorum yorum = new isYorum();
            yorum.isYorumAciklama = aciklama;
            yorum.isYorumKayitTarih = DateTime.Now;
            yorum.isYorumRefYorumuYapanID = kullaniciId;
            yorum.isYorumRefislerID = isId;
            Db.isYorums.Add(yorum);
            Db.SaveChanges();
            return Json(jsn,JsonRequestBehavior.AllowGet);
        }
        public JsonResult DegistirSatisOncelik(int domainId, bool value)
        {
            JsonCevap jsn = new JsonCevap();
            var domain = Db.Domains.SingleOrDefault(x=>x.DomainID==domainId);
            domain.SatisOncelikli = value;
            Db.Database.ExecuteSqlCommand(@"update isler set islerOncelikSiraID=@p0 
                                         where islerRefDomainID=@p1",(int)IslerOncelikSira.Birinci,domainId);
           
            Db.SaveChanges();
            return Json(jsn, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DegistirGuncellemeSozlesmesi(int domainId, bool value)
        {
            JsonCevap jsn = new JsonCevap();
            var domain = Db.Domains.SingleOrDefault(x => x.DomainID == domainId);
            domain.GuncellemeSozlesmesiVarmi = value;
            Db.SaveChanges();
            return Json(jsn, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DegistirOdemesiAlindi(int domainId, bool value)
        {
            JsonCevap jsn = new JsonCevap();
            var domain = Db.Domains.SingleOrDefault(x => x.DomainID == domainId);
            domain.OdemesiAlindi = value;
            Db.SaveChanges();
            return Json(jsn, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DomainAksiyonDegistir(int domainId, int domainAksiyon)
        {
            JsonCevap jsn = new JsonCevap();
            DomainAksiyon yeniAksiyon = (DomainAksiyon)domainAksiyon;
            var domain = Db.Domains.SingleOrDefault(x => x.DomainID == domainId);
            if(yeniAksiyon==DomainAksiyon.BeklemeyeAl || yeniAksiyon == DomainAksiyon.YayiniDurdur)
            {
                Db.Database.ExecuteSqlCommand( @" UPDATE dbo.isler  
SET     islerIsinDurumu =  CASE when  
								islerIsinDurumu=3 
								then 
								case when
									islerBitisTarihiVarmi=1
									then 2--deadline
									else 1--yapilacak
								end
						   end
WHERE   islerRefDomainID=@p0 and islerIsinDurumu=3",domainId);
            }
            domain.DomainAksiyon = (int)yeniAksiyon;
            Db.SaveChanges();
            jsn.Basarilimi = true;
            return Json(jsn, JsonRequestBehavior.AllowGet);
        }
        #endregion 
        public ActionResult IcerikFormu()
        {
           
            return View();
        }
        [ValidateInput(false)]
        [HttpPost]
        public ActionResult IcerikFormu(IcerikFormu icerik, FormCollection frm)
        {
            // is atanacak ve kontrol edecek kullanici ayar tablosundaki kayitlara gore belirleniyor
            // Ayar table da IcerikFormuIsAtanacakKullanici ve IcerikFormuIsKontrolEdenKullanici kayitlari yok ise ekranda uyari gosteriyor
            var isAtanacakKullanici = Db.Ayars.Where(x=>x.AyarAdi== "IcerikFormuIsAtanacakKullanici").SingleOrDefault();
            var isKontrolEdenKullanici = Db.Ayars.Where(x=>x.AyarAdi== "IcerikFormuIsKontrolEdenKullanici").SingleOrDefault();
            if (ModelState.IsValid && (isAtanacakKullanici!=null && isKontrolEdenKullanici!=null) &&
                (!string.IsNullOrEmpty(isAtanacakKullanici.AyarDeger) && !(string.IsNullOrEmpty(isKontrolEdenKullanici.AyarDeger)) ))
            {
                var listStandardIsler = Db.StandartProjeIsleris.ToList().OrderBy(x => x.StandartProjeIsleriSirasi);
                var dinamiStandartIsler = listStandardIsler.Where(x => x.StandartProjeIsleriIdAnahtarIsmi != null);
                var isHtml = string.Format("<p>Firma Adı:{0}</p>",icerik.FirmaAdi);
                isHtml += string.Format("<p>Domain Adı:{0}</p>", icerik.DomainAdi);
                isHtml += string.Format("<p>Telefon 1:{0}</p>", icerik.Telefon1);
                isHtml += string.Format("<p>Telefon 2:{0}</p>", icerik.Telefon2);
                isHtml += string.Format("<p>Email:{0}</p>", icerik.Email);
                isHtml += string.Format("<p>Adres:{0}</p>", icerik.Adres);
                isHtml += string.Format("<p>Konum Adı:{0}</p>", icerik.Konum);
                isHtml += string.Format("<p>Instagram Adı:{0}</p>", icerik.Instagram);
                isHtml += string.Format("<p>Google Plus Adı:{0}</p>", icerik.GooglePlus);
                isHtml += string.Format("<p>Twitter:{0}</p>", icerik.Twitter);
                foreach (var dinamikIs in dinamiStandartIsler)
                {
                    string anahtar = dinamikIs.StandartProjeIsleriIdAnahtarIsmi + "Alindi";
                    if (frm[anahtar].Contains("true"))
                    {
                        isHtml += string.Format("<p>{0} Alındı:{1}</p>", dinamikIs.StandartProjeIsleriIdAnahtarIsmi,frm[anahtar]);
                    }else
                    {
                        isHtml += string.Format("<p>{0} Alınmadı</p>", dinamikIs.StandartProjeIsleriIdAnahtarIsmi);
                    }
                }

                isler ilkIs = new isler();
                ilkIs.islerAciklama = string.Format("Domain:{0}-Firma:{1}", icerik.DomainAdi, icerik.FirmaAdi);
                ilkIs.islerAdi=icerik.DomainAdi+" bilgileri";
                ilkIs.islerRefDomainID = icerik.DomainId;
                ilkIs.islerRefFirmaID = icerik.FirmaId;
                ilkIs.islerisiYapacakKisi = isAtanacakKullanici.AyarDeger;
                //degişebilir
                ilkIs.islerisiVerenKisi = isKontrolEdenKullanici.AyarDeger;
                ilkIs.islerTarih = DateTime.Now;
                ilkIs.islerOncelikSiraID =(int) IslerOncelikSira.Ikinci;
                //List<isler> isler = new List<Models.isler>();
                //isler.Add(ilkIs);
                ilkIs.islerSiraNo = 1;
                ilkIs.islerIsinDurumu = (int)IsinDurumu.Yapilacak;
                Db.islers.Add(ilkIs);
                int siraNo = 2;
                foreach (var standardIs in listStandardIsler)
                {
                    isler job = new isler();
                    job.islerAciklama = string.Format("{0}", standardIs.StandartProjeIsleriAciklama);
                    job.islerAdi =standardIs.StandartProjeIsleriIsAdi;
                    job.islerRefDomainID = icerik.DomainId;
                    job.islerRefFirmaID = icerik.FirmaId;
                    job.islerisiYapacakKisi = standardIs.RefStandartProjeIsleriYapacakKullaniciId;
                    job.islerOncelikSiraID = (int)IslerOncelikSira.Ikinci;
                    job.islerisiVerenKisi = standardIs.RefStandartProjeIsleriKontrolEdecekKullaniciId;
                    job.islerTarih = DateTime.Now;
                    job.islerSiraNo = siraNo;
                    job.islerIsinDurumu = (int)IsinDurumu.Yapilacak;
                    siraNo = siraNo + 1;
                    IsiYapacakKisi kisi = new IsiYapacakKisi();
                    kisi.RefIsiYapacakKisiUserID = standardIs.RefStandartProjeIsleriYapacakKullaniciId;
                    kisi.isler = job;
                    Db.IsiYapacakKisis.Add(kisi);
                    Db.islers.Add(job);
                }
                //Db.islers.AddRange(isler);
                Db.SaveChanges();
                TempData["Success"] = "Kaydedildi";

                return RedirectToAction("Index",new { kontrolBekleyenIsler=false,  onaylananIsler=false });

            }
            else
            {
                TempData["Error"] = "Ayar tablosuna IcerikFormuIsAtanacakKullanici ve IcerikFormuIsKontrolEdenKullanici kayıtlarnı giriniz .";
                return View();
            }
          


         
        }
        [ValidateInput(false)]
        public ActionResult IcerikKaydet(string json)
        {
            IcerikFormu m = JsonConvert.DeserializeObject<IcerikFormu>(json);
            JsonCevap cevap = new JsonCevap();
            cevap.Basarilimi = true;

            return Json(cevap, JsonRequestBehavior.AllowGet);
        }
        public JsonResult FirmalariGetir(string firmaAdi)
        {


            var firmalar = new List<object>();

            foreach (Firma firma in Db.Firmas.Where(a => a.FirmaAdi.Contains(firmaAdi) || a.YetkiliCepTelefon.Contains(firmaAdi) 
            || a.YetkiliTelefon.Contains(firmaAdi) || a.Email.Contains(firmaAdi)
            ))
            {
                firmalar.Add(new
                {
                    value = firma.FirmaID, label = firma.FirmaAdi,Telefon1=firma.YetkiliTelefon,Telefon2=firma.YetkiliCepTelefon,
                    Email=firma.Email,Adres=firma.FirmaAdres,Kayitlimi=true,Adi=firma.YetkiliAdi,Soyadi=firma.YetkiliSoyAdi,
                    FirmaSahibiOzellik="",Sehir=firma.firmaSehir,ilce=firma.firmailce,WebAdresi="",
                    KonumId =firma.RefKonumID,SektorId=firma.firmaSektorID,DomainKategoriId=firma.firmaDomainKategoriID
                });
            }


            var diziArayanlar = Db.Arayanlars.Where(a => a.arayanKayitliMusterimi == false && a.arayanFirmaAdi.Contains(firmaAdi) || a.arayanCepTelNo.Contains(firmaAdi)
            || a.arayanTelefonNo.Contains(firmaAdi) || a.arayanMailAdresi.Contains(firmaAdi) ).ToList();

            List<int> listTemp = new List<int>();
           
            //.Select(o => new { ArayanAdi = o.Key, Arayan = o.OrderBy(c => c.arayanFirmaAdi).ToList() }).ToList();

            foreach (var arayan in diziArayanlar)
            {
                //Arayanlar arayanFirma = db.Arayanlars.FirstOrDefault(a => a.arayanFirmaAdi == aramaYapan.ArayanAdi);
                //if (arayanFirma != null)
                //{
                //    firmalar.Add(new { value = "kayitliDegil" + arayanFirma.arayanID, label = arayanFirma.arayanFirmaAdi + " - Kayıtlı Değil" });
                //}
                if (!listTemp.Contains(arayan.arayanID))
                {
                    firmalar.Add(new { value = arayan.arayanID, label = arayan.arayanFirmaAdi,Telefon1=arayan.arayanTelefonNo,Telefon2=arayan.arayanCepTelNo,
                        Email = arayan.arayanMailAdresi,Adres = arayan.arayanAdres,Kayitlimi = false,
                        Adi = arayan.arayanAdi,Soyadi = arayan.arayanSoyadi,FirmaSahibiOzellik = arayan.arayanFirmaSahibiOzelligi,
                        Sehir = arayan.arayanSehir,ilce = arayan.arayanilce, WebAdresi = arayan.arayanWebAdresi,
                        KonumId = arayan.arayanRefKonumID,SektorId = arayan.arayanSektorID,
                        DomainKategoriId = arayan.arayanDomainKategoriID
                    });
                    listTemp.Add(arayan.arayanID);
                }
               
            }

            return Json(firmalar, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DomainGetir(string domainAdi,int? firmaId)
        {
        

            var listDomain = (from d in Db.Domains
                              where (firmaId.HasValue ? d.RefDomainFirmaID == firmaId.Value : true) && d.DomainAdi.Contains(domainAdi)
                              select new
                              {
                                  value = d.DomainID,label=d.DomainAdi,firmaId=d.Firma.FirmaID,firmaAdi=d.Firma.FirmaAdi,
                                  Telefon1 = d.Firma.YetkiliTelefon,
                                  Telefon2 = d.Firma.YetkiliCepTelefon,
                                  Email = d.Firma.Email,
                                  Adres = d.Firma.FirmaAdres
                              }).ToList();

          

            return Json(listDomain, JsonRequestBehavior.AllowGet);
        }
    }
   
}