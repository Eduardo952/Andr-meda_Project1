using Ares.Domain;

namespace Ares.UI
{
    /// <summary>Presentation-only priorities. Advice must describe actions that actually exist in the game.</summary>
    internal readonly struct OxygenGuidance
    {
        public readonly string Title, Advice;
        public readonly bool Warning;
        OxygenGuidance(string title, string advice, bool warning = false)
        { Title = title; Advice = advice; Warning = warning; }
        public static OxygenGuidance Read(OxygenPlant plant, Phase phase)
        {
            if (plant.WaterKg <= 0)
                return new OxygenGuidance("A água do gerador acabou", plant.LaboratoryEnabled
                    ? "O gerador parou. Pause os experimentos para poupar o oxigênio guardado."
                    : "Os experimentos já estão pausados. Sem água, aumentar o ritmo não ajuda.", true);
            if (plant.PowerFraction < .99f)
                return new OxygenGuidance("O gerador precisa de energia", "Limpe os painéis solares. Sem energia, o gerador e o filtro trabalham menos.", true);
            if (plant.Critical)
                return new OxygenGuidance("A reserva está baixa", plant.Balance > 0
                    ? "A reserva já está se recuperando. Acompanhe o tanque e evite gastos extras."
                    : "Escolha Reforçar e pause os experimentos para ajudar a reserva.", true);
            if (phase == Phase.Plan)
                return new OxygenGuidance("Tudo pronto para começar", "Escolha o ritmo do gerador. Depois, use a seta para planejar a energia do dia.");
            if (plant.Balance < -.00001f)
                return new OxygenGuidance("Estamos usando a reserva", plant.Mode == OxygenMode.Boost
                    ? "Pause os experimentos para reduzir o uso de oxigênio."
                    : "Escolha Equilibrar ou Reforçar para produzir mais oxigênio.", true);
            if (plant.Percent >= 99.9f)
                return new OxygenGuidance("Temos uma boa reserva", plant.Mode == OxygenMode.Boost
                    ? "O tanque está cheio. Experimente Equilibrar para gastar menos água e energia."
                    : "O gerador acompanha o uso da equipe. Você pode manter este ritmo.");
            if (plant.Balance > .00001f)
                return new OxygenGuidance("A reserva está aumentando", "Estamos produzindo mais do que usamos. O que sobra fica guardado.");
            return new OxygenGuidance("A reserva está estável", "Estamos produzindo tanto quanto usamos. Continue acompanhando o tanque.");
        }
    }
}
