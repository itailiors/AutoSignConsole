using System;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Blockcore.NBitcoin;
using Blockcore.NBitcoin.DataEncoders;
using NBitcoin;
using NBitcoin.DataEncoders;
using System.Net.WebSockets;
using Network = Blockcore.Networks.Network;


class Program
{
    static async Task Main(string[] args)
    {
        string relayUrl = "wss://relay.angor.io"; // Replace with actual relay URL
        using var client = new ClientWebSocket();

        try
        {
            Console.WriteLine($"Connecting to relay: {relayUrl}");
            await client.ConnectAsync(new Uri(relayUrl), CancellationToken.None);
            Console.WriteLine("Connected to relay!");

            var buffer = new byte[1024 * 4];

            while (client.State == WebSocketState.Open)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Message received: {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
        }
    }

    private static void HandleRelayMessage(string message)
    {
        try
        {
            var signatureRequest = System.Text.Json.JsonSerializer.Deserialize<SignatureRequest>(message);

            if (signatureRequest != null && !string.IsNullOrEmpty(signatureRequest.EncryptedMessage))
            {
                Console.WriteLine($"Processing signature request from {signatureRequest.InvestorNostrPubKey}...");
                AutoSignRequest(signatureRequest);
            }
            else
            {
                Console.WriteLine("Invalid message or no encrypted content found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
        }
    }

    private static void AutoSignRequest(SignatureRequest request)
    {
        try
        {
            string privateKey = "15839d7dc2355aad183c4c4ad6efdced46550146be2a2a5a0b35141bb75123cc"; // Replace with actual private key
            string decryptedTransactionHex = DecryptNostrContent(privateKey, request.InvestorNostrPubKey, request.EncryptedMessage);

            if (!string.IsNullOrEmpty(decryptedTransactionHex))
            {
                Console.WriteLine($"Decrypted Transaction Hex: {decryptedTransactionHex}");

                string signedTransaction = SignTransaction(decryptedTransactionHex, privateKey);

                if (!string.IsNullOrEmpty(signedTransaction))
                {
                    Console.WriteLine($"Signed Transaction: {signedTransaction}");
                    SendSignedTransaction(request, signedTransaction);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error signing request: {ex.Message}");
        }
    }

    private static string DecryptNostrContent(string nsec, string npub, string encryptedContent)
    {
        string sharedSecretHex = GetSharedSecretHexWithoutPrefix(nsec, npub);
        return DecryptWithSharedSecret(encryptedContent, sharedSecretHex);
    }

    private static string GetSharedSecretHexWithoutPrefix(string nsec, string npub)
    {
        var privateKey = new Blockcore.NBitcoin.Key(Blockcore.NBitcoin.DataEncoders.Encoders.Hex.DecodeData(nsec));
        var publicKey = new Blockcore.NBitcoin.PubKey("02" + npub);

        var sharedSecret = publicKey.GetSharedPubkey(privateKey);
        return Blockcore.NBitcoin.DataEncoders.Encoders.Hex.EncodeData(sharedSecret.ToBytes()[1..]);
    }


    private static string DecryptWithSharedSecret(string encryptedContent, string sharedSecretHex)
    {
        var key = Convert.FromHexString(sharedSecretHex);
        var combined = Convert.FromBase64String(encryptedContent);

        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[16];
        var ciphertext = new byte[combined.Length - 16];
        Array.Copy(combined, 0, iv, 0, 16);
        Array.Copy(combined, 16, ciphertext, 0, ciphertext.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var decryptedBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static string? SignTransaction(string transactionHex, string privateKeyHex)
    {
        try
        {
            // Fully qualify the 'Key' and 'Encoders' to avoid ambiguity.
            var key = new Blockcore.NBitcoin.Key(Blockcore.NBitcoin.DataEncoders.Encoders.Hex.DecodeData(privateKeyHex));
            var testNet = NBitcoin.Network.TestNet;

            // Ensure the correct Network is being used. Replace with the appropriate Network reference.
            var transaction = NBitcoin.Transaction.Parse(transactionHex, );

            // Sign the transaction
            transaction.Sign(key, true);
            return transaction.ToHex();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error signing transaction: {ex.Message}");
            return null;
        }
    }

    

    private static async Task SendSignedTransaction(SignatureRequest request, string signedTransaction)
    {
        string relayUrl = "wss://relay.angor.io"; // Replace with your relay URL
        using var ws = new ClientWebSocket();

        try
        {
            // Create the response object
            var response = new
            {
                @event = "signed",
                signedTransaction,
                request.EventId
            };

            // Serialize the response to JSON
            string jsonResponse = System.Text.Json.JsonSerializer.Serialize(response);

            // Connect to the WebSocket relay
            Console.WriteLine($"Connecting to relay: {relayUrl}");
            await ws.ConnectAsync(new Uri(relayUrl), CancellationToken.None);
            Console.WriteLine("Connected to relay!");

            // Send the JSON response
            var buffer = Encoding.UTF8.GetBytes(jsonResponse);
            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
            Console.WriteLine($"Signed transaction sent for EventId: {request.EventId}");

            // Optional: Wait for an acknowledgment or response from the relay
            var receiveBuffer = new byte[1024 * 4];
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

            // Log any acknowledgment or response
            string ackMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
            Console.WriteLine($"Acknowledgment received: {ackMessage}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendSignedTransaction: {ex.Message}");
        }
        finally
        {
            if (ws.State == WebSocketState.Open)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
                Console.WriteLine("WebSocket connection closed.");
            }
        }
    }

}

public class SignatureRequest
{
    public string? InvestorNostrPubKey { get; set; }
    public string? EncryptedMessage { get; set; }
    public string? EventId { get; set; }
}
