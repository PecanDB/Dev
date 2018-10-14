using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

/*
 Encrypt File
 =========================
Encrypt a file using the FileEncrypt method that expects as first argument the path to the file that will be encrypted and as second argument the password that will be used to encrypt it. The password can be used to decrypt the file later. To make everything right, we recommend you to delete the password from the memory using the ZeroMemory method. Call this function to remove the key from memory after use for security purposes:

string password = "ThePasswordToDecryptAndEncryptTheFile";

// For additional security Pin the password of your files
GCHandle gch = GCHandle.Alloc(password, GCHandleType.Pinned);

// Encrypt the file
FileEncrypt(@"C:\Users\username\Desktop\wordFileExample.doc", password);

// To increase the security of the encryption, delete the given password from the memory !
ZeroMemory(gch.AddrOfPinnedObject(), password.Length * 2);
gch.Free();

// You can verify it by displaying its value later on the console (the password won't appear)
Console.WriteLine("The given password is surely nothing: " + password);
 Copy snippet
The FileEncrypt method will generate a file in the same directory of the original file with the aes extension (e.g wordFileExample.doc).

Decrypt File
=======================
To decrypt the file, we'll follow the same process but using FileDecrypt instead. This method expects as first argument the path to the encrypted file and as second argument the path where the decrypted file should be placed. As third argument you need to provide the string that was used to encrypt the file originally:

string password = "ThePasswordToDecryptAndEncryptTheFile";

// For additional security Pin the password of your files
GCHandle gch = GCHandle.Alloc(password, GCHandleType.Pinned);

// Decrypt the file
FileDecrypt(@"C:\Users\sdkca\Desktop\example.doc.aes", @"C:\Users\sdkca\Desktop\example_decrypted.doc", password);

// To increase the security of the decryption, delete the used password from the memory !
ZeroMemory(gch.AddrOfPinnedObject(), password.Length * 2);
gch.Free();

// You can verify it by displaying its value later on the console (the password won't appear)
Console.WriteLine("The given password is surely nothing: " + password);

     */

public class FileEncryptionFactory
{
    //  Call this function to remove the key from memory after use for security
    [DllImport("KERNEL32.DLL", EntryPoint = "RtlZeroMemory")]
    public static extern bool ZeroMemory(IntPtr Destination, int Length);

    /// <summary>
    ///     Creates a random salt that will be used to encrypt your file. This method is required on FileEncrypt.
    /// </summary>
    /// <returns></returns>
    public static byte[] GenerateRandomSalt()
    {
        var data = new byte[32];

        using (var rng = new RNGCryptoServiceProvider())
        {
            for (int i = 0; i < 10; i++)
                // Fille the buffer with the generated data
                rng.GetBytes(data);
        }

        return data;
    }

    /// <summary>
    ///     Encrypts a file from its path and a plain password.
    /// </summary>
    /// <param name="inputFile"></param>
    /// <param name="password"></param>
    private void FileEncrypt(string inputFile, string password)
    {
        //http://stackoverflow.com/questions/27645527/aes-encryption-on-large-files

        //generate random salt
        byte[] salt = GenerateRandomSalt();

        //create output file name
        var fsCrypt = new FileStream(inputFile + ".aes", FileMode.Create);

        //convert password string to byte arrray
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        //Set Rijndael symmetric encryption algorithm
        var AES = new RijndaelManaged();
        AES.KeySize = 256;
        AES.BlockSize = 128;
        AES.Padding = PaddingMode.PKCS7;

        //http://stackoverflow.com/questions/2659214/why-do-i-need-to-use-the-rfc2898derivebytes-class-in-net-instead-of-directly
        //"What it does is repeatedly hash the user password along with the salt." High iteration counts.
        var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
        AES.Key = key.GetBytes(AES.KeySize / 8);
        AES.IV = key.GetBytes(AES.BlockSize / 8);

        //Cipher modes: http://security.stackexchange.com/questions/52665/which-is-the-best-cipher-mode-and-padding-mode-for-aes-encryption
        AES.Mode = CipherMode.CFB;

        // write salt to the begining of the output file, so in this case can be random every time
        fsCrypt.Write(salt, 0, salt.Length);

        var cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);

        var fsIn = new FileStream(inputFile, FileMode.Open);

        //create a buffer (1mb) so only this amount will allocate in the memory and not the whole file
        var buffer = new byte[1048576];

        try
        {
            int read;
            while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                //MediaTypeNames.Application.DoEvents(); // -> for responsive GUI, using Task will be better!
                cs.Write(buffer, 0, read);

            // Close up
            fsIn.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            cs.Close();
            fsCrypt.Close();
        }
    }

    /// <summary>
    ///     Decrypts an encrypted file with the FileEncrypt method through its path and the plain password.
    /// </summary>
    /// <param name="inputFile"></param>
    /// <param name="outputFile"></param>
    /// <param name="password"></param>
    private void FileDecrypt(string inputFile, string outputFile, string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        var salt = new byte[32];

        var fsCrypt = new FileStream(inputFile, FileMode.Open);
        fsCrypt.Read(salt, 0, salt.Length);

        var AES = new RijndaelManaged();
        AES.KeySize = 256;
        AES.BlockSize = 128;
        var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
        AES.Key = key.GetBytes(AES.KeySize / 8);
        AES.IV = key.GetBytes(AES.BlockSize / 8);
        AES.Padding = PaddingMode.PKCS7;
        AES.Mode = CipherMode.CFB;

        var cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);

        var fsOut = new FileStream(outputFile, FileMode.Create);

        var buffer = new byte[1048576];

        try
        {
            int read;
            while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                // MediaTypeNames.Application.DoEvents();
                fsOut.Write(buffer, 0, read);
        }
        catch (CryptographicException ex_CryptographicException)
        {
            Console.WriteLine("CryptographicException error: " + ex_CryptographicException.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        try
        {
            cs.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error by closing CryptoStream: " + ex.Message);
        }
        finally
        {
            fsOut.Close();
            fsCrypt.Close();
        }
    }
}