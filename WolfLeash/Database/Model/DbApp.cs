using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WolfLeash.Database.Model;

[Index(nameof(AppId), IsUnique = true)]
public class DbApp
{
    [Key]
    public int Id { get; set; }
    [Required(ErrorMessage = "Title is required", AllowEmptyStrings = false), StringLength(64)]
    public string Title { get; set; } = null!;
    [Required, StringLength(64)]
    public string AppId { get; set; } = null!;
    [StringLength(256)]
    public string? IconPngPath { get; set; }
    public bool? SupportHdr { get; set; }
    public bool? StartVirtualCompositor { get; set; }
    public bool? StartAudioServer { get; set; }

    [Required]
    public Runner Runner { get; set; } = null!;

    [StringLength(128)]
    public string? RenderNode { get; set; } = null!;

    public string? H264_gst_pipeline { get; set; }
    public string? Hevc_gst_pipeline { get; set; }
    public string? Av1_gst_pipeline { get; set; }
    public string? Opus_gst_pipeline { get; set; }
}