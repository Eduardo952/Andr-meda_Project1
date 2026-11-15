# Rega manual — manutenção

As regras e a base científica estão em [Casa Marte](../Domain/GREENHOUSE.md). A estufa preserva a mesma reserva durante os cinco dias.

## Fluxo de interação

1. Perto da entrada, clique no regador para pegar. E continua disponível como alternativa.
2. Perto do reservatório, clique nele ou pressione E para completar até 510 mL. A retirada é descontada imediatamente.
3. Observe o solo no HUD ou no painel. Perto da prateleira, clique nela: uma dose começa e termina em 2,5 segundos. Não é necessário segurar o botão.
4. O regador vazio precisa ser abastecido antes de outra dose. Solo seco exige duas doses separadas; solo muito úmido não recebe água.
5. Clique no suporte ou pressione E para devolver. A água já carregada permanece no regador.

Cliques precisam atingir o volume do alvo e respeitar a proximidade, os menus e o modo de controle do jogador. Clicar no chão não rega. `TryClick(Ray)` é o adaptador verificável; não depende de regras de consumo duplicadas na UI. A rega é interrompida ao sair da área, abrir um menu, perder o foco ou devolver o regador. A dose só é debitada ao completar a ação: cancelar não perde água nem conta uma dose parcial.

## Livro de água

```text
Reservatório + água no regador + total aplicado nas plantas = 11.000 mL
```

`GreenhouseSimulation.Withdraw` transfere água do reservatório. `GreenhouseWatering.Fill` limita a retirada ao espaço do regador. `DeliverDose` e o débito no regador ocorrem juntos no fim da ação. O último abastecimento pode ser parcial: menos de 510 mL não permite uma rega completa. Retornar, reabrir menus e trocar de dia não criam nem descartam água.

## Cena

Coordenadas Unity:

| Ponto de aproximação | Posição |
| --- | --- |
| Regador | `(35,0,-2)` |
| Reservatório | `(48,0,-2.8)` |
| Prateleiras 1 e 2 | `(43,0,-4.5)` e `(61,0,-4.5)` |
| Prateleiras 3 e 4 | `(43,0,4.5)` e `(61,0,4.5)` |

Os cultivos ficam 2,5 m além desses pontos no eixo Z, afastando-se do corredor. `GreenhouseCropRacks` substitui visualmente os antigos interiores de hidroponia importados, preservando seus assets e restaurando os renderers ao ser destruído. São quatro grupos, com dez recipientes identificáveis em cada grupo; malhas são agrupadas por material. O código reaproveita o utilitário de malhas `RoverMesh`, sem dependência dos controles do rover.

`GreenhouseWorldView` cuida do regador e dos indicadores. `GreenhouseOcclusion` continua responsável pelo recorte de visibilidade que deixa o jogador visível. `GreenhouseCare` cria o modelo antes de registrar os objetos na oclusão e libera recursos em ordem inversa. Alterações de posição devem manter modelo, volumes de clique e indicadores sincronizados.

Para verificar esta funcionalidade, use o menu focado descrito no documento de domínio. O antigo ponto de entrada `ManualWateringChecks` redireciona para essa verificação; os testes dos antigos níveis foram substituídos.
