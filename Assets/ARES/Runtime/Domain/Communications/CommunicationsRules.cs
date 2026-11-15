namespace Ares.Domain.Communications
{
    /// <summary>Educational radio rules, not flight hardware specifications or live ephemerides.</summary>
    public static class CommunicationsRules
    {
        public const int MissionDays = 5;
        public const float OneWayGameSeconds = 12;
        public const float ShortReplyKwh = .2f, HelpReplyKwh = .5f;
        public const float ReportKwh = .2f, ReportBatteryKwh = 1;
        public const float RepairStepSeconds = 2;
        public const int AntennaFaultDay = 3;
        public static string Guidance(int day)
        {
            switch (day)
            {
                case 2: return "Observe o solo antes de regar. Uma reserva de água bem cuidada ajuda nos próximos dias.";
                case 3: return "Uma antena pode precisar de manutenção. Se o sinal cair, confira o cabo, alinhe a antena e teste a conexão.";
                case 4: return "Energia é compartilhada: conversar com a Terra também usa a bateria que ajuda a manter o ar da base.";
                case 5: return "Último dia! Confira as plantas, a energia e a proteção. Seu relatório conta como foi a missão.";
                default: return "Olá, Ares Base! Somos a equipe da Terra nesta aventura. Cuide do ar, da energia e das plantas. No fim do dia, envie seu relatório.";
            }
        }
        public static string Help(int day) => day == 2 ? "Solo moderado: uma rega. Seco: duas. Muito úmido: nenhuma. Reabasteça o regador entre as doses."
            : day == 3 ? "Para recuperar o sinal, abra a manutenção da antena. Cada etapa precisa terminar antes da próxima."
            : "Observe primeiro. Limpe os painéis para recuperar geração de energia e evite gastar mais do que precisa. As reservas devem durar toda a missão.";
    }
}
