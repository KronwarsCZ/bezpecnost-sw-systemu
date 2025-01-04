namespace backend.entities;

public class Credential
{
    public Guid Id { get; set; }
    public byte[] CredentialId { get; set; }
    public byte[] PublicKey { get; set; }
    
    public uint SignCounter { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }
        
}