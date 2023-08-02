namespace telbot.handle;
using telbot.models;
public class HandleInformation
{
  private HandleMessage bot;
  private Request request;
  private UsersModel user;
  private Configuration cfg;
  private List<string> respostas;
  private DateTime agora;
  public HandleInformation(HandleMessage bot, Configuration cfg, UsersModel user, Request request)
  {
    this.bot = bot;
    this.cfg = cfg;
    this.user = user;
    this.request = request;
    this.agora = DateTime.Now;
    respostas = telbot.Temporary.executar(cfg, this.request.aplicacao!, this.request.informacao!);
  }
  async public Task routeInformation()
  {
    if(respostas.Count == 0)
    {
      var erro = new Exception("Erro no script do SAP");
      await bot.ErrorReport(id: user.id, request: request, error: erro);
      return;
    }
    if(respostas[0].StartsWith("ERRO"))
    {
      var erro = new Exception("Erro no script do SAP");
      await bot.ErrorReport(id: user.id, error: erro, request: request, respostas[0]);
      return;
    }
    switch (request.aplicacao)
    {
      case "telefone":await SendManuscripts(); break;
      case "coordenada":await SendManuscripts(); break;
      case "localizacao":await SendManuscripts(); break;
      case "leiturista":await SendPicture(); break;
      case "roteiro":await SendPicture(); break;
      case "fatura":await SendDocument(); break;
      case "debito":await SendDocument(); break;
      case "historico":await SendPicture(); break;
      case "contato":await SendManuscripts(); break;
      case "agrupamento":await SendPicture(); break;
      case "pendente":await SendPicture(); break;
      case "relatorio":await SendWorksheet(); break;
      case "manobra":await SendWorksheet(); break;
      case "medidor":await SendManuscripts(); break;
      case "passivo":await SendDocument(); break;
      case "suspenso":await SendManuscripts(); break;
    }
    return;
  }
  // Para envio de texto simples
  async public Task SendManuscripts()
  {
    string textoMensagem = String.Empty;
    foreach (var resposta in respostas)
    {
      textoMensagem += resposta;
      textoMensagem += "\n";
    }
    await bot.sendTextMesssageWraper(user.id, textoMensagem);
    Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true));
    return;
  }
  // Para envio de faturas em PDF
  async public Task SendDocument()
  {
    if(cfg.GERAR_FATURAS == false)
    {
      await bot.sendTextMesssageWraper(user.id, "O sistema SAP não está gerando faturas no momento!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false));
      return;
    }
    if(request.aplicacao == "passivo" && (DateTime.Today.DayOfWeek == DayOfWeek.Friday || DateTime.Today.DayOfWeek == DayOfWeek.Saturday))
    {
      await bot.sendTextMesssageWraper(user.id, "Essa aplicação não deve ser usada na sexta e no sábado!");
      await bot.sendTextMesssageWraper(user.id, "Notas de recorte devem ter todas as faturas cobradas!");
      return;
    }
    try
    {
      foreach (string fatura in respostas)
      {
        if (fatura == "None" || fatura == null || fatura == "")
        {
          continue;
        }
        await using Stream stream = System.IO.File.OpenRead(@$"{cfg.CURRENT_PATH}\tmp\{fatura}");
        await bot.SendDocumentAsyncWraper(user.id, stream, fatura);
        stream.Dispose();
        await bot.sendTextMesssageWraper(user.id, fatura, false);
      }
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true));
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(id: user.id, request: request, error: error);
    }
    return;
  }
  // Para envio de relatórios
  async public Task SendPicture()
  {
    try
    {
      telbot.Temporary.executar(cfg, respostas);
      await using Stream stream = System.IO.File.OpenRead(@$"{cfg.CURRENT_PATH}\tmp\temporario.png");
      await bot.SendPhotoAsyncWraper(user.id, stream);
      stream.Dispose();
      System.IO.File.Delete(@$"{cfg.CURRENT_PATH}\tmp\temporario.png");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true));
      if((request.aplicacao == "agrupamento") && (DateTime.Today.DayOfWeek == DayOfWeek.Friday))
      await bot.sendTextMesssageWraper(user.id, "*ATENÇÃO:* Não pode cortar agrupamento por nota de recorte!");
      await bot.sendTextMesssageWraper(user.id, $"Enviado relatorio de {request.aplicacao}!", false);
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(id: user.id, request: request, error: error);
    }
    return;
  }
  // Para envio de planilhas
  async public Task SendWorksheet()
  {
    if(user.has_privilege == false)
    {
      await bot.sendTextMesssageWraper(user.id, "Você não tem permissão para gerar relatórios!");
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, false));
      return;
    }
    try
    {
      await using Stream stream = System.IO.File.OpenRead(@"C:\Users\ruan.camello\SapWorkDir\export.XLSX");
      await bot.SendDocumentAsyncWraper(user.id, stream, $"{agora.ToString("yyyyMMdd_HHmmss")}.XLSX");
      stream.Dispose();
      System.IO.File.Delete(@"C:\Users\ruan.camello\SapWorkDir\export.XLSX");
      await bot.sendTextMesssageWraper(user.id, $"Enviado arquivo de {request.aplicacao}: {agora.ToString("yyyyMMdd_HHmmss")}.XLSX", false);
      Database.inserirRelatorio(new logsModel(user.id, request.aplicacao, request.informacao, true));
    }
    catch (System.Exception error)
    {
      await bot.ErrorReport(id: user.id, request: request, error: error);
    }
    return;
  }
}