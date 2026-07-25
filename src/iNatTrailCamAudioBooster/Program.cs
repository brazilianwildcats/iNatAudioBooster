using System.Diagnostics;

namespace INatTrailCamAudioBooster;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (_, e) =>
        {
            AppLog.WriteException("Erro não tratado da interface", e.Exception);
            MessageBox.Show(
                $"O aplicativo encontrou um erro inesperado.\n\n{e.Exception.Message}\n\nLog:\n{AppPaths.LogFile}",
                "iNat TrailCam Audio Booster",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                AppLog.WriteException("Erro não tratado do aplicativo", ex);
        };

        try
        {
            AppLog.Initialize();
            AppLog.Write("Aplicativo iniciado.");
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            AppLog.WriteException("Falha fatal na inicialização", ex);
            MessageBox.Show(
                $"Não foi possível iniciar o aplicativo.\n\n{ex.Message}\n\nLog:\n{AppPaths.LogFile}",
                "iNat TrailCam Audio Booster",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            AppLog.Write("Aplicativo encerrado.");
        }
    }
}
