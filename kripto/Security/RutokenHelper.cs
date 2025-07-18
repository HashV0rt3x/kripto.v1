using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using Net.Pkcs11Interop.HighLevelAPI40;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.AccessControl;
using System.Text;

namespace kripto.Security
{
    /// <summary>
    /// RuToken ma'lumotlari modeli
    /// </summary>
    public class TokenData
    {
        public string Label { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public bool IsModifiable { get; set; }
        public int SizeBytes { get; set; }
        public ulong ObjectId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// RuToken bilan ishlash uchun yordamchi sinf (.NET 9 optimized)
    /// </summary>
    public class RutokenHelper : IDisposable
    {
        private IPkcs11Library? pkcs11Library;
        private ISession? session;
        private bool isInitialized = false;
        private bool isDisposed = false;

        // RuToken library paths (Windows)
        private static readonly string[] PossibleLibraryPaths =
        [
            @"C:\Windows\System32\rtpkcs11ecp.dll",
            @"C:\Windows\SysWOW64\rtpkcs11ecp.dll",
            @"rtpkcs11ecp.dll",
            @"rtpkcs11.dll"
        ];

        /// <summary>
        /// Rutoken'ni ishga tushirish
        /// </summary>
        public bool Initialize(string pin)
        {
            try
            {
                if (isInitialized)
                    return true;

                if (string.IsNullOrEmpty(pin))
                {
                    throw new ArgumentException("PIN bo'sh bo'lishi mumkin emas", nameof(pin));
                }

                // Library path'ni topish
                string libraryPath = FindLibraryPath();
                System.Diagnostics.Debug.WriteLine($"Using RuToken library: {libraryPath}");

                // PKCS#11 kutubxonasini yuklash
                pkcs11Library = new Pkcs11InteropFactories().Pkcs11LibraryFactory.LoadPkcs11Library(
                    new Pkcs11InteropFactories(),
                    libraryPath,
                    AppType.MultiThreaded);

                // Slotlarni qidirish
                var slots = pkcs11Library.GetSlotList(SlotsType.WithTokenPresent);
                if (slots.Count == 0)
                {
                    throw new Exception("Hech qanday token topilmadi. Token ulangan va ishlayotganini tekshiring.");
                }

                GetAllTokens();

                // Birinchi mavjud slotdan sessiya ochish
                var slot = slots[0];
                session = slot.OpenSession(SessionType.ReadWrite);

                // PIN bilan kirish
                session.Login(CKU.CKU_USER, pin);

                isInitialized = true;
                System.Diagnostics.Debug.WriteLine("✅ RuToken muvaffaqiyatli ishga tushirildi");
                return true;
            }
            catch (Pkcs11Exception pkcs11Ex)
            {
                string errorMessage = GetPkcs11ErrorMessage(pkcs11Ex);
                System.Diagnostics.Debug.WriteLine($"❌ PKCS#11 xatosi: {errorMessage}");
                throw new Exception(errorMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Rutoken'ni ishga tushirishda xatolik: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ {errorMessage}");
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Library path'ni topish
        /// </summary>
        private static string FindLibraryPath()
        {
            foreach (string path in PossibleLibraryPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Found RuToken library: {path}");
                    return path;
                }
            }

            throw new Exception("RuToken library topilmadi. Iltimos, RuToken driver'ni o'rnating.\n" +
                              "Quyidagi path'lardan birida library bo'lishi kerak:\n" +
                              string.Join("\n", PossibleLibraryPaths));
        }

        /// <summary>
        /// PKCS#11 xatolik xabarlarini tushunarli qilish
        /// </summary>
        private static string GetPkcs11ErrorMessage(Pkcs11Exception pkcs11Ex)
        {
            string baseMessage = $"PKCS#11 xatosi: {pkcs11Ex.Message}";

            string additionalInfo = pkcs11Ex.RV switch
            {
                CKR.CKR_PIN_INCORRECT => "\nPIN noto'g'ri. Default PIN: 12345678",
                CKR.CKR_TOKEN_NOT_PRESENT => "\nToken ulanmagan yoki tanilmagan",
                CKR.CKR_PIN_LOCKED => "\nPIN bloklangan. Administrator bilan bog'laning",
                CKR.CKR_USER_NOT_LOGGED_IN => "\nFoydalanuvchi tizimga kirmagan",
                CKR.CKR_SESSION_CLOSED => "\nSession yopilgan",
                CKR.CKR_DEVICE_ERROR => "\nQurilma xatosi",
                CKR.CKR_CRYPTOKI_NOT_INITIALIZED => "\nCryptoki ishga tushirilmagan",
                _ => ""
            };

            return baseMessage + additionalInfo;
        }

        /// <summary>
        /// Barcha custom tokenlarni olish
        /// </summary>
        public List<TokenData> GetAllTokens()
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            try
            {
                EnsureInitialized();

                // Custom tokenlarni qidirish
                var searchTemplate = new List<IObjectAttribute>
                {
                    session!.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true)
                };

                var foundObjects = session.FindAllObjects(searchTemplate);
                var tokens = new List<TokenData>();

                foreach (var obj in foundObjects)
                {
                    try
                    {
                        var tokenData = ReadTokenData(obj);
                        if (tokenData != null)
                        {
                            tokens.Add(tokenData);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Token o'qishda xatolik: {ex.Message}");
                        // Continue with other tokens
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Found {tokens.Count} tokens");
                return tokens;
            }
            catch (Exception ex)
            {
                throw new Exception($"Tokenlarni o'qishda xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Berilgan label bo'yicha tokenni qidirish
        /// </summary>
        public TokenData GetTokenByLabel(string label)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            try
            {
                EnsureInitialized();

                if (string.IsNullOrEmpty(label))
                {
                    throw new ArgumentException("Label bo'sh bo'lishi mumkin emas", nameof(label));
                }

                // Label bo'yicha qidirish
                var searchTemplate = new List<IObjectAttribute>
                {
                    session!.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, label)
                };

                var foundObjects = session.FindAllObjects(searchTemplate);

                if (foundObjects.Count == 0)
                {
                    throw new Exception($"'{label}' nomli token topilmadi!");
                }

                // Birinchi topilgan tokenni qaytarish
                var tokenData = ReadTokenData(foundObjects[0]);
                if (tokenData == null)
                {
                    throw new Exception($"'{label}' token ma'lumotlarini o'qib bo'lmadi!");
                }

                return tokenData;
            }
            catch (Exception ex)
            {
                throw new Exception($"Token qidirishda xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Token ma'lumotlarini o'qish
        /// </summary>
        private TokenData? ReadTokenData(IObjectHandle objectHandle)
        {
            try
            {
                var attributes = session!.GetAttributeValue(objectHandle, new List<CKA>
                {
                    CKA.CKA_LABEL,
                    CKA.CKA_VALUE,
                    CKA.CKA_PRIVATE,
                    CKA.CKA_MODIFIABLE
                });

                var tokenData = new TokenData
                {
                    Label = attributes[0].GetValueAsString() ?? "Unnamed",
                    Data = Encoding.UTF8.GetString(attributes[1].GetValueAsByteArray() ?? []),
                    IsPrivate = attributes[2].GetValueAsBool(),
                    IsModifiable = attributes[3].GetValueAsBool(),
                    SizeBytes = attributes[1].GetValueAsByteArray()?.Length ?? 0,
                    ObjectId = objectHandle.ObjectId
                };

                return tokenData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReadTokenData xatolik: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Yangi token yaratish
        /// </summary>
        public void CreateToken(string label, string data)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            try
            {
                EnsureInitialized();

                if (string.IsNullOrEmpty(label))
                {
                    throw new ArgumentException("Label bo'sh bo'lishi mumkin emas", nameof(label));
                }

                if (string.IsNullOrEmpty(data))
                {
                    throw new ArgumentException("Data bo'sh bo'lishi mumkin emas", nameof(data));
                }

                var dataBytes = Encoding.UTF8.GetBytes(data);

                var objectAttributes = new List<IObjectAttribute>
                {
                    session!.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_PRIVATE, false),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_MODIFIABLE, true),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, label),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_VALUE, dataBytes)
                };

                var createdObject = session.CreateObject(objectAttributes);

                System.Diagnostics.Debug.WriteLine($"✅ Token created: {label} (ID: {createdObject.ObjectId})");
            }
            catch (Exception ex)
            {
                throw new Exception($"Token yaratishda xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Token ma'lumotlarini yangilash
        /// </summary>
        public void UpdateTokenData(string label, string newData)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            try
            {
                EnsureInitialized();

                if (string.IsNullOrEmpty(label))
                {
                    throw new ArgumentException("Label bo'sh bo'lishi mumkin emas", nameof(label));
                }

                if (string.IsNullOrEmpty(newData))
                {
                    throw new ArgumentException("NewData bo'sh bo'lishi mumkin emas", nameof(newData));
                }

                // Tokenni topish
                var searchTemplate = new List<IObjectAttribute>
                {
                    session!.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, label)
                };

                var foundObjects = session.FindAllObjects(searchTemplate);

                if (foundObjects.Count == 0)
                {
                    throw new Exception($"'{label}' nomli token topilmadi!");
                }

                var targetObject = foundObjects[0];
                var newDataBytes = Encoding.UTF8.GetBytes(newData);

                // Ma'lumotni yangilash
                var updateAttributes = new List<IObjectAttribute>
                {
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_VALUE, newDataBytes)
                };

                session.SetAttributeValue(targetObject, updateAttributes);

                System.Diagnostics.Debug.WriteLine($"✅ Token updated: {label}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Token yangilashda xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Token o'chirish
        /// </summary>
        public void DeleteToken(string label)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            try
            {
                EnsureInitialized();

                if (string.IsNullOrEmpty(label))
                {
                    throw new ArgumentException("Label bo'sh bo'lishi mumkin emas", nameof(label));
                }

                // Tokenni topish
                var searchTemplate = new List<IObjectAttribute>
                {
                    session!.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_DATA),
                    session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, label)
                };

                var foundObjects = session.FindAllObjects(searchTemplate);

                if (foundObjects.Count == 0)
                {
                    throw new Exception($"'{label}' nomli token topilmadi!");
                }

                var targetObject = foundObjects[0];
                session.DestroyObject(targetObject);

                System.Diagnostics.Debug.WriteLine($"✅ Token deleted: {label}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Token o'chirishda xatolik: {ex.Message}");
            }
        }

        /// <summary>
        /// Token ma'lumotlarini olish (umumiy metod)
        /// </summary>
        public string GetTokenInfo()
        {
            try
            {
                if (!isInitialized || pkcs11Library == null)
                {
                    return "❌ Rutoken ishga tushirilmagan";
                }

                var slots = pkcs11Library.GetSlotList(SlotsType.WithTokenPresent);
                if (slots.Count == 0)
                {
                    return "❌ Token topilmadi";
                }

                var tokenInfo = slots[0].GetTokenInfo();
                return $"📱 Model: {tokenInfo.Model}\n" +
                       $"🏷️ Label: {tokenInfo.Label}\n" +
                       $"🔢 Serial: {tokenInfo.SerialNumber}\n" +
                       $"🏭 Manufacturer: {tokenInfo.ManufacturerId}";
            }
            catch (Exception ex)
            {
                return $"❌ Token ma'lumotlarini olishda xatolik: {ex.Message}";
            }
        }

        /// <summary>
        /// RuToken holatini tekshirish
        /// </summary>
        public bool IsHealthy()
        {
            try
            {
                if (!isInitialized || session == null || pkcs11Library == null)
                {
                    return false;
                }

                // Simple health check - try to get slots
                var slots = pkcs11Library.GetSlotList(SlotsType.WithTokenPresent);
                return slots.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Session ma'lumotlarini olish
        /// </summary>
        public string GetSessionInfo()
        {
            try
            {
                if (!isInitialized || session == null)
                {
                    return "❌ Session mavjud emas";
                }

                var sessionInfo = session.GetSessionInfo();
                return $"🔐 Session State: {sessionInfo.State}\n" +
                       $"🎯 Device Error: {sessionInfo.DeviceError}\n" +
                       $"🔑 Flags: {sessionInfo.SessionFlags}";
            }
            catch (Exception ex)
            {
                return $"❌ Session ma'lumotlarini olishda xatolik: {ex.Message}";
            }
        }

        /// <summary>
        /// Initialization holatini tekshirish
        /// </summary>
        private void EnsureInitialized()
        {
            if (!isInitialized || session == null)
            {
                throw new Exception("RuToken ishga tushirilmagan. Avval Initialize() metodini chaqiring.");
            }
        }

        /// <summary>
        /// Resurslarni tozalash
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            try
            {
                if (session != null)
                {
                    try
                    {
                        session.Logout();
                    }
                    catch (Pkcs11Exception ex) when (ex.RV == CKR.CKR_USER_NOT_LOGGED_IN)
                    {
                        // Already logged out, ignore
                    }
                    catch
                    {
                        // Ignore logout errors during disposal
                    }

                    session.CloseSession();
                    session.Dispose();
                    session = null;
                }

                if (pkcs11Library != null)
                {
                    pkcs11Library.Dispose();
                    pkcs11Library = null;
                }

                isInitialized = false;
                System.Diagnostics.Debug.WriteLine("✅ RuToken resources disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error during disposal: {ex.Message}");
            }
            finally
            {
                isDisposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~RutokenHelper()
        {
            Dispose();
        }
    }
}