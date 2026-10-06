// Rød Måne – iOS-hjælpefunktioner til Unity-versionen.
// - RedMoonKeychain_*: gemmer appens krypteringsnøgle og session-token i iOS Keychain
//   med kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly (følger aldrig med i backup eller til en ny telefon).
// - RedMoonFile_Protect: udelukker datafilen fra iCloud-backup og slår NSFileProtectionComplete til.
// Kaldes fra C# via [DllImport("__Internal")] (se App/Platform/KeyStores.cs og PlatformStorage.cs).

#import <Foundation/Foundation.h>
#import <Security/Security.h>
#include <stdlib.h>
#include <string.h>

static NSString* const RedMoonKeychainService = @"dk.roedmaane.unity";

static NSMutableDictionary* RedMoonKeychainQuery(const char* key)
{
    NSMutableDictionary* query = [NSMutableDictionary dictionary];
    query[(__bridge id)kSecClass] = (__bridge id)kSecClassGenericPassword;
    query[(__bridge id)kSecAttrService] = RedMoonKeychainService;
    query[(__bridge id)kSecAttrAccount] = [NSString stringWithUTF8String:key];
    return query;
}

extern "C" int RedMoonKeychain_Set(const char* key, const char* value)
{
    if (key == NULL || value == NULL) return -1;

    NSMutableDictionary* query = RedMoonKeychainQuery(key);
    SecItemDelete((__bridge CFDictionaryRef)query);

    query[(__bridge id)kSecValueData] = [[NSString stringWithUTF8String:value] dataUsingEncoding:NSUTF8StringEncoding];
    query[(__bridge id)kSecAttrAccessible] = (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly;
    return (int)SecItemAdd((__bridge CFDictionaryRef)query, NULL);
}

// Returnerer en kopi (malloc) af værdien, eller NULL. C# frigiver den med RedMoonKeychain_Free.
extern "C" char* RedMoonKeychain_Get(const char* key)
{
    if (key == NULL) return NULL;

    NSMutableDictionary* query = RedMoonKeychainQuery(key);
    query[(__bridge id)kSecReturnData] = @YES;
    query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;

    CFTypeRef result = NULL;
    OSStatus status = SecItemCopyMatching((__bridge CFDictionaryRef)query, &result);
    if (status != errSecSuccess || result == NULL) return NULL;

    NSData* data = (__bridge NSData*)result;
    char* copy = (char*)malloc(data.length + 1);
    if (copy != NULL)
    {
        memcpy(copy, data.bytes, data.length);
        copy[data.length] = '\0';
    }
    CFRelease(result);
    return copy;
}

extern "C" int RedMoonKeychain_Remove(const char* key)
{
    if (key == NULL) return -1;
    return (int)SecItemDelete((__bridge CFDictionaryRef)RedMoonKeychainQuery(key));
}

extern "C" void RedMoonKeychain_Free(char* pointer)
{
    free(pointer);
}

extern "C" void RedMoonFile_Protect(const char* path)
{
    if (path == NULL) return;
    NSString* filePath = [NSString stringWithUTF8String:path];
    NSURL* url = [NSURL fileURLWithPath:filePath];
    [url setResourceValue:@YES forKey:NSURLIsExcludedFromBackupKey error:nil];
    [[NSFileManager defaultManager] setAttributes:@{ NSFileProtectionKey: NSFileProtectionComplete }
                                     ofItemAtPath:filePath
                                            error:nil];
}
