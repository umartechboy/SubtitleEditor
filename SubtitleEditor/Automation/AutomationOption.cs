using Microsoft.AspNetCore.Components.Forms;
using OpenTK.Graphics.OpenGL;

namespace SubtitleEditor.Automation
{
    public class AutomationOption
    {
        public string AcceptedFormats { get; set; } = "";
        public string Label { get; set; } = "";
        public string LoadedDataLabel { get; set; } = "";
        public byte[] Data { get; set; }
        public string DefaultDataSource { get; set; } = "";
        public bool HasDefault { get; set; } = true;
        public async Task FileSelected(IBrowserFile file)
        {
            var data = new byte[file.Size];
            await file.OpenReadStream(20000000).ReadAsync(data);
            Data = data;
            LoadedDataLabel = file.Name;
        }
        public async Task DefaultSelected(HttpClient http)
        {
            try
            {
                var resp = await http.GetAsync(DefaultDataSource);
                if (!resp.IsSuccessStatusCode)
                {
                    LoadedDataLabel = "Failed to download";
                    Data = new byte[0];
                    return;
                }
                LoadedDataLabel = Path.GetFileName(DefaultDataSource);
                Data = await resp.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex) 
            {
            }
        }
    }
}
