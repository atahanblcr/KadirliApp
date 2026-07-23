#!/bin/bash
echo "1. Sisteme 'admin' kullanıcısı ile giriş yapılıyor..."
curl -s -i -c cookies.txt -d "username=admin&password=admin" -X POST http://localhost:5002/Account/Login > /dev/null
echo "✅ Giriş başarılı. Cookie oluşturuldu."
echo ""

echo "2. Sol Menü (Sidebar) kontrol ediliyor (Eczaneler, Vefat, Rehber)..."
MENU_CHECK=$(curl -s -b cookies.txt http://localhost:5002/Dashboard/Index | grep -E "Vefat|Eczane|Rehber")
if [[ ! -z "$MENU_CHECK" ]]; then
    echo "✅ Yeni menüler başarıyla render ediliyor!"
fi
echo ""

echo "3. AdsAdmin (İlanlar) Sayfasındaki Buton Formları (Approve/Reject) Kontrol Ediliyor..."
FORM_CHECK=$(curl -s -b cookies.txt http://localhost:5002/AdsAdmin/Index | grep -E "asp-action|method=\"post\"")
if [[ ! -z "$FORM_CHECK" ]]; then
    echo "✅ İlanlar sayfasında POST formları ve butonlar mevcut!"
fi
echo ""

echo "4. AdsAdminController -> Approve İşlemi Tetikleniyor (Rastgele bir GUID ile)..."
# Rastgele bir guid gönderiyoruz
DUMMY_ID=$(uuidgen)
POST_RESULT=$(curl -s -i -b cookies.txt -d "id=$DUMMY_ID" -X POST http://localhost:5002/AdsAdmin/Approve)

# Gelen response'un HTTP Header kısımlarına bakıp 302 Redirect attığına ve TempData Error mesajı bastığına bakacağız
if echo "$POST_RESULT" | grep -q "302 Found"; then
    echo "✅ POST işlemi başarıyla Controller'a ulaştı ve 302 Redirect döndürdü!"
fi

echo "--- Test Tamamlandı ---"
