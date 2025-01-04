namespace backend.entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string SuperSecret { get; set; }
    public IEnumerable<Credential> Credentials { get; set; } = new List<Credential>();
}