This folder contains a fork of the SSH.NET library's Renci.SshNet project.

It is based on:
URL: https://sshnet.svn.codeplex.com/svn/Renci.SshClient/Renci.SshNet
Revision: 21102

Effective working copy status is:
Revision: 19813
Date: 2012-09-13 08:12:54

Any modifications and additions that were implemented for the Remote Terminal
project were not contributed back to the SSH.NET repository.

Any modifications that were implemented in the SSH.NET library after forking
it were not integrated into the fork.

Here are some of the modifications that were implemented to this library
for the sake of Remote Terminal:
 * added PrivateKeyAgent implementation (OpenSSH-style)
 * removed support for proxy servers and port forwarding
   this was done to reduce the complexity of porting the library to WinRT
 * necessary changes because of limited/different WinRT APIs, e.g.
   * Thread.Sleep(...) => Task.Delay(...)
   * Timer -> ThreadPoolTimer
   * RNGCryptoServiceProvider.GetBytes(...) => CryptographicBuffer.GenerateRandom(...)
   * new XXXHash() => HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.XXX)
   * new HMac<XXXHash>(...) => MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacXXX).CreateKey(...)
   * this.GetType().GetCustomAttributes(...) => this.GetType().GetTypeInfo().GetCustomAttributes(...)
   * HashAlgorithm => CryptographicKey
   * Socket => StreamSocket, DataReader, DataWriter
