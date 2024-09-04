using telbot.Services;
using telbot.models;
namespace telbot.Helpers;
public static partial class PdfHandle
{
  public static async void Watch()
  {
    var cfg = Configuration.GetInstance();
    var database = Database.GetInstance();
    ConsoleWrapper.Debug(Entidade.SoireeAsync, "Monitor de faturas iniciado!");
    while (true)
    {
      try
      {
        await Task.Delay(cfg.TASK_DELAY);
        var files = System.IO.Directory.GetFiles(cfg.TEMP_FOLDER);
        foreach (var file in files)
        {
          if(System.IO.Path.GetExtension(file) != ".pdf") continue;
          var filename = System.IO.Path.GetFileName(file);
          var registry = database.RecuperarFatura(filename);
          if(registry == null)
          {
            var instalation = PdfHandle.Check(file);
            var timestamp = System.IO.File.GetLastWriteTime(file);
            var fatura = new pdfsModel() {
              filename = filename,
              instalation = instalation,
              timestamp = timestamp,
              status = pdfsModel.Status.wait
            };
            var fatura_txt = System.Text.Json.JsonSerializer.Serialize<pdfsModel>(fatura);
            ConsoleWrapper.Debug(Entidade.SoireeAsync, fatura_txt);
            database.InserirFatura(fatura);
          }
          else
          {
            if(registry.status == pdfsModel.Status.sent)
            {
              System.IO.File.Delete(filename);
              registry.status = pdfsModel.Status.done;
              database.AlterarFatura(registry);
            }
          }
        }
      }
      catch (System.Exception erro)
      {
        ConsoleWrapper.Error(Entidade.Executor, erro);
      }
    }
  }
}