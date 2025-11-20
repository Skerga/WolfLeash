using System.ComponentModel.DataAnnotations;

namespace WolfLeash.Database.Model;

public class Client
{
    [Key]
    public string Id { get; set; }
    [StringLength(64)]
    public string Alias { get; set; } = string.Empty;
    public BlazorBootstrap.IconName? Icon { get; set; }
}