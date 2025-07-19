using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kripto.Helpers
{
    public class RuToken
    {
        const string RUTOKEN_MODULE = @"C:\Windows\System32\rtPKCS11ECP.dll";
        const string PIN = "12345678";

        static ISession session;
        static IPkcs11Library pkcs11Library;

        /// <summary>
        /// Rutoken ga ulanish va sessiya ochish
        /// </summary>
        public static bool InitializeRutoken()
        {
            try
            {
                Console.WriteLine("1️⃣ PKCS#11 kutubxonasini yuklash...");

                pkcs11Library = new Pkcs11InteropFactories().Pkcs11LibraryFactory.LoadPkcs11Library(
                    new Pkcs11InteropFactories(),
                    RUTOKEN_MODULE,
                    AppType.MultiThreaded);

                Console.WriteLine("✅ Kutubxona yuklandi");

                Console.WriteLine("2️⃣ Slotlarni qidirish...");

                var slots = pkcs11Library.GetSlotList(SlotsType.WithTokenPresent);
                if (slots.Count == 0)
                {
                    Console.WriteLine("❌ Hech qanday token topilmadi!");
                    Console.WriteLine("💡 Token ulangan va to'g'ri ishlayotganini tekshiring");
                    return false;
                }

                Console.WriteLine($"✅ {slots.Count} ta slot topildi");

                Console.WriteLine("3️⃣ Token ma'lumotlarini o'qish...");

                var slot = slots[0];
                var tokenInfo = slot.GetTokenInfo();

                Console.WriteLine($"   📱 Model: {tokenInfo.Model}");
                Console.WriteLine($"   🏷️  Label: {tokenInfo.Label}");
                Console.WriteLine($"   🔢 Serial: {tokenInfo.SerialNumber}");
                Console.WriteLine($"   🏭 Manufacturer: {tokenInfo.ManufacturerId}");

                Console.WriteLine("4️⃣ Sessiya ochish...");

                session = slot.OpenSession(SessionType.ReadWrite);
                Console.WriteLine("✅ Sessiya ochildi");

                Console.WriteLine("5️⃣ PIN bilan kirish...");

                session.Login(CKU.CKU_USER, PIN);
                Console.WriteLine("✅ Muvaffaqiyatli kirildi");

                Console.WriteLine($"\n🎉 {tokenInfo.Label} ({tokenInfo.SerialNumber}) ga muvaffaqiyatli ulandi!\n");

                return true;
            }
            catch (Pkcs11Exception pkcs11Ex)
            {
                Console.WriteLine($"❌ PKCS#11 xatosi: {pkcs11Ex.Message}");
                Console.WriteLine($"🔢 Xato kodi: {pkcs11Ex.RV}");

                switch (pkcs11Ex.RV)
                {
                    case CKR.CKR_PIN_INCORRECT:
                        Console.WriteLine("💡 PIN noto'g'ri. Default PIN: 12345678");
                        break;
                    case CKR.CKR_TOKEN_NOT_PRESENT:
                        Console.WriteLine("💡 Token ulanmagan yoki tanilmagan");
                        break;
                    case CKR.CKR_PIN_LOCKED:
                        Console.WriteLine("💡 PIN bloklangan. Adminstratorga murojaat qiling");
                        break;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Umumiy xatolik: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Custom token yozish
        /// </summary>
        public static void WriteCustomToken()
        {
            try
            {
                Console.WriteLine("\n📝 === CUSTOM TOKEN YOZISH === 📝");

                Console.Write("🏷️ Token nomi (label): ");
                var label = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(label))
                {
                    Console.WriteLine("❌ Token nomi bo'sh bo'lishi mumkin emas!");
                    return;
                }

                Console.Write("📄 Token ma'lumoti (data): ");
                var data = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(data))
                {
                    Console.WriteLine("❌ Token ma'lumoti bo'sh bo'lishi mumkin emas!");
                    return;
                }

                Console.Write("🔐 Token private bo'lsinmi? (y/n): ");
                var isPrivate = Console.ReadLine()?.ToLower() == "y";

                Console.WriteLine("💾 Token yozilmoqda...");

                var dataBytes = Encoding.UTF8.GetBytes(data);
                var attributes = new List<IObjectAttribute>
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_PRIVATE, isPrivate),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, label),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_VALUE, dataBytes),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_MODIFIABLE, true)
                };

                var createdObject = session.CreateObject(attributes);

                Console.WriteLine("✅ Token muvaffaqiyatli yozildi!");
                Console.WriteLine($"   🏷️ Nomi: {label}");
                Console.WriteLine($"   📄 Ma'lumot: {data}");
                Console.WriteLine($"   🔐 Private: {(isPrivate ? "Ha" : "Yo'q")}");
                Console.WriteLine($"   📏 Hajmi: {dataBytes.Length} bayt");
                Console.WriteLine($"   🆔 Object ID: {createdObject.ObjectId}");
            }
            catch (Pkcs11Exception pkcs11Ex)
            {
                Console.WriteLine($"❌ PKCS#11 xatosi: {pkcs11Ex.Message}");
                Console.WriteLine($"🔢 Xato kodi: {pkcs11Ex.RV}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Custom token o'qish
        /// </summary>
        public static void ReadCustomToken()
        {
            try
            {
                Console.WriteLine("\n📖 === CUSTOM TOKEN O'QISH === 📖");

                Console.Write("🏷️ Token nomi (label): ");
                var label = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(label))
                {
                    Console.WriteLine("❌ Token nomi bo'sh bo'lishi mumkin emas!");
                    return;
                }

                Console.WriteLine("🔍 Token qidirilmoqda...");

                var searchTemplate = new List<IObjectAttribute>
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, label)
                };

                var foundObjects = session.FindAllObjects(searchTemplate);

                if (foundObjects.Count == 0)
                {
                    Console.WriteLine($"❌ '{label}' nomli token topilmadi!");
                    return;
                }

                if (foundObjects.Count > 1)
                {
                    Console.WriteLine($"⚠️ Bir nechta token topildi ({foundObjects.Count} ta):");
                    for (int i = 0; i < foundObjects.Count; i++)
                    {
                        Console.WriteLine($"   {i + 1}. Object ID: {foundObjects[i].ObjectId}");
                    }
                    Console.Write("🔢 Qaysi birini tanlaysiz? (1-" + foundObjects.Count + "): ");
                    if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= foundObjects.Count)
                    {
                        ReadSingleToken(foundObjects[choice - 1]);
                    }
                    else
                    {
                        Console.WriteLine("❌ Noto'g'ri tanlov!");
                    }
                }
                else
                {
                    ReadSingleToken(foundObjects[0]);
                }
            }
            catch (Pkcs11Exception pkcs11Ex)
            {
                Console.WriteLine($"❌ PKCS#11 xatosi: {pkcs11Ex.Message}");
                Console.WriteLine($"🔢 Xato kodi: {pkcs11Ex.RV}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Bitta tokenni o'qish
        /// </summary>
        public static void ReadSingleToken(IObjectHandle objectHandle)
        {
            try
            {
                var attributes = session.GetAttributeValue(objectHandle, new List<CKA>
                {
                    CKA.CKA_LABEL,
                    CKA.CKA_VALUE,
                    CKA.CKA_PRIVATE,
                    CKA.CKA_MODIFIABLE
                });

                var tokenLabel = attributes[0].GetValueAsString();
                var tokenData = Encoding.UTF8.GetString(attributes[1].GetValueAsByteArray());
                var isPrivate = attributes[2].GetValueAsBool();
                var isModifiable = attributes[3].GetValueAsBool();

                Console.WriteLine("✅ Token muvaffaqiyatli o'qildi!");
                Console.WriteLine($"   🏷️ Nomi: {tokenLabel}");
                Console.WriteLine($"   📄 Ma'lumot: {tokenData}");
                Console.WriteLine($"   🔐 Private: {(isPrivate ? "Ha" : "Yo'q")}");
                Console.WriteLine($"   ✏️ O'zgartirilishi mumkin: {(isModifiable ? "Ha" : "Yo'q")}");
                Console.WriteLine($"   📏 Hajmi: {attributes[1].GetValueAsByteArray().Length} bayt");
                Console.WriteLine($"   🆔 Object ID: {objectHandle.ObjectId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Token o'qishda xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Barcha tokenlarni ko'rish
        /// </summary>
        public static void ListAllTokens()
        {
            try
            {
                Console.WriteLine("\n📋 === BARCHA TOKENLAR === 📋");

                var searchTemplate = new List<IObjectAttribute>
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true)
                };

                var foundObjects = session.FindAllObjects(searchTemplate);

                if (foundObjects.Count == 0)
                {
                    Console.WriteLine("❌ Hech qanday custom token topilmadi!");
                    return;
                }

                Console.WriteLine($"📦 Jami {foundObjects.Count} ta token topildi:\n");

                for (int i = 0; i < foundObjects.Count; i++)
                {
                    try
                    {
                        var attributes = session.GetAttributeValue(foundObjects[i], new List<CKA>
                        {
                            CKA.CKA_LABEL,
                            CKA.CKA_VALUE,
                            CKA.CKA_PRIVATE
                        });

                        var label = attributes[0].GetValueAsString();
                        var dataLength = attributes[1].GetValueAsByteArray().Length;
                        var isPrivate = attributes[2].GetValueAsBool();

                        Console.WriteLine($"{i + 1}. 🏷️ {label}");
                        Console.WriteLine($"   📏 Hajmi: {dataLength} bayt");
                        Console.WriteLine($"   🔐 Private: {(isPrivate ? "Ha" : "Yo'q")}");
                        Console.WriteLine($"   🆔 Object ID: {foundObjects[i].ObjectId}");
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{i + 1}. ❌ Token o'qishda xatolik: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Tokenlarni ro'yxatlashda xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Token o'chirish
        /// </summary>
        public static void DeleteCustomToken()
        {
            try
            {
                Console.WriteLine("\n🗑️ === CUSTOM TOKEN O'CHIRISH === 🗑️");

                Console.Write("🏷️ O'chiriladigan token nomi (label): ");
                var label = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(label))
                {
                    Console.WriteLine("❌ Token nomi bo'sh bo'lishi mumkin emas!");
                    return;
                }

                Console.WriteLine("🔍 Token qidirilmoqda...");

                var searchTemplate = new List<IObjectAttribute>
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, label)
                };

                var foundObjects = session.FindAllObjects(searchTemplate);

                if (foundObjects.Count == 0)
                {
                    Console.WriteLine($"❌ '{label}' nomli token topilmadi!");
                    return;
                }

                Console.WriteLine($"⚠️ {foundObjects.Count} ta token topildi. Rostdan ham o'chirmoqchimisiz?");
                Console.Write("🔢 Tasdiqlash uchun 'DELETE' yozing: ");
                var confirmation = Console.ReadLine();

                if (confirmation != "DELETE")
                {
                    Console.WriteLine("❌ O'chirish bekor qilindi!");
                    return;
                }

                Console.WriteLine("🗑️ Tokenlar o'chirilmoqda...");

                int deletedCount = 0;
                foreach (var obj in foundObjects)
                {
                    try
                    {
                        session.DestroyObject(obj);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Object {obj.ObjectId} o'chirishda xatolik: {ex.Message}");
                    }
                }

                Console.WriteLine($"✅ {deletedCount} ta token muvaffaqiyatli o'chirildi!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Token o'chirishda xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Token holati haqida ma'lumot
        /// </summary>
        public static void GetTokenStatus()
        {
            try
            {
                Console.WriteLine("\n📊 === TOKEN HOLATI === 📊");

                if (session == null)
                {
                    Console.WriteLine("❌ Sessiya ochilmagan");
                    return;
                }

                var sessionInfo = session.GetSessionInfo();
                Console.WriteLine($"🔗 Sessiya ID: {sessionInfo.SessionId}");
                Console.WriteLine($"📱 Slot ID: {sessionInfo.SlotId}");
                Console.WriteLine($"🔐 Holat: {sessionInfo.State}");
                Console.WriteLine($"⚠️ Device Error: {sessionInfo.DeviceError}");

                // Custom tokenlar soni
                var customTokens = session.FindAllObjects(new List<IObjectAttribute>
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true)
                });

                Console.WriteLine($"📦 Custom tokenlar: {customTokens.Count} ta");

                // Barcha obyektlar soni
                var allObjects = session.FindAllObjects(new List<IObjectAttribute>
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true)
                });

                Console.WriteLine($"📋 Jami obyektlar: {allObjects.Count} ta");

                // Xotira holati
                var slots = pkcs11Library.GetSlotList(SlotsType.WithTokenPresent);
                if (slots.Count > 0)
                {
                    var tokenInfo = slots[0].GetTokenInfo();
                    Console.WriteLine($"💾 Xotira (Free): {tokenInfo.FreePrivateMemory} bayt");
                    Console.WriteLine($"💾 Xotira (Total): {tokenInfo.TotalPrivateMemory} bayt");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Token holatini olishda xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Ulanish testi
        /// </summary>
        public static void TestConnection()
        {
            try
            {
                Console.WriteLine("\n🔬 === ULANISH TESTI === 🔬");

                // Test token yozish
                Console.WriteLine("1️⃣ Test token yozish...");
                var testLabel = $"TEST_{DateTime.Now:yyyyMMdd_HHmmss}";
                var testData = $"Test ma'lumot - {DateTime.Now}";

                if (WriteTestToken(testLabel, testData))
                {
                    Console.WriteLine("✅ Yozish muvaffaqiyatli");

                    // Test token o'qish
                    Console.WriteLine("2️⃣ Test token o'qish...");
                    if (ReadTestToken(testLabel))
                    {
                        Console.WriteLine("✅ O'qish muvaffaqiyatli");

                        // Test token o'chirish
                        Console.WriteLine("3️⃣ Test token o'chirish...");
                        if (DeleteTestToken(testLabel))
                        {
                            Console.WriteLine("✅ O'chirish muvaffaqiyatli");
                        }
                    }
                }

                Console.WriteLine("\n🎉 Barcha testlar muvaffaqiyatli bajarildi!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test xatosi: {ex.Message}");
            }
        }

        public static bool WriteTestToken(string label, string data)
        {
            try
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var attributes = new List<IObjectAttribute>
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_PRIVATE, false),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, label),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_VALUE, dataBytes)
                };

                session.CreateObject(attributes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ReadTestToken(string label)
        {
            try
            {
                var searchTemplate = new List<IObjectAttribute>
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, label)
                };

                var foundObjects = session.FindAllObjects(searchTemplate);
                if (foundObjects.Count > 0)
                {
                    var attributes = session.GetAttributeValue(foundObjects[0], new List<CKA> { CKA.CKA_VALUE });
                    var data = Encoding.UTF8.GetString(attributes[0].GetValueAsByteArray());
                    Console.WriteLine($"   📄 O'qilgan ma'lumot: {data}");
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteTestToken(string label)
        {
            try
            {
                var searchTemplate = new List<IObjectAttribute>
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, label)
                };

                var foundObjects = session.FindAllObjects(searchTemplate);
                if (foundObjects.Count > 0)
                {
                    session.DestroyObject(foundObjects[0]);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resurslarni tozalash
        /// </summary>
       public static void Cleanup()
        {
            try
            {
                Console.WriteLine("\n🧹 Resurslarni tozalash...");

                if (session != null)
                {
                    session.Logout();
                    Console.WriteLine("✅ Sessiyadan chiqildi");

                    session.CloseSession();
                    Console.WriteLine("✅ Sessiya yopildi");
                }

                if (pkcs11Library != null)
                {
                    pkcs11Library.Dispose();
                    Console.WriteLine("✅ Kutubxona tozalandi");
                }

                Console.WriteLine("✅ Barcha resurslar tozalandi");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Tozalashda ogohlantirish: {ex.Message}");
            }
        }
    }
}