# Portas de regresso — um mundo, três reinos

16 outubro 2026. Correção feita pelo plugin oficial Unity CLI/Pipeline.

**Organização atual:** [espaço exterior partilhado](Espaco_Exterior_Compartilhado.md). A cópia `Scenes/Worlds/FirstJourney_FantasyKingdom_Conteudo.unity` conserva estas três portas e regressa ao asteroide situado a 180 m da base, através da sessão persistente. As coordenadas das casas no mundo continuam iguais. A versão standalone anterior permanece preservada.

## Visão confirmada

O asteroide leva a **um mundo**, Fantasy Kingdom, com três reinos/regiões no mesmo mapa. `Composition_A`, `Composition_B` e `Composition_C` continuam na mesma cena. A clarificação final do criador substitui a resposta anterior sobre viagens diretas entre reinos: não foi criado esse sistema. O regresso ao asteroide é feito pela porta de uma casa.

## O que foi corrigido

O ponto anterior de regresso estava junto de uma banca, sem porta visível. O teste inicial provava a transição nesse ponto, mas não a utilização da porta real da casa; foi uma validação insuficiente para o percurso apresentado ao jogador.

Agora há uma porta funcional na casa correspondente de cada região. É a porta lateral, junto da banca com toldo vermelho, sobre os degraus. Uma placa **ASTEROIDE**, luz quente e marcador de distância permitem identificá-la. Aproximar-se e premir **E** ou **Quadrado** (`buttonWest`) abre a folha para fora e inicia o regresso. A interação funciona do chão diante dos degraus, sem exigir um salto para a soleira. A porta da casa no asteroide continua a levar ao mesmo mundo.

| Região | Porta no mundo | Aproximação no chão |
| --- | --- | --- |
| 1 / Composition_A | (5.560, 1.080, 17.200) | (8.300, 0.400, 17.200) |
| 2 / Composition_B | (187.895, 1.102, 287.698) | (187.989, 0.422, 284.960) |
| 3 / Composition_C | (-135.180, 1.110, 397.308) | (-133.936, 0.430, 394.866) |

As folhas e molduras reutilizam as peças originais `ANG_Porta_*`, sobre as portas existentes, sem modificar os modelos fonte do Fantasy Kingdom. A superfície luminosa aparece durante a abertura; não existe ainda um interior de casa explorável. Não foram alterados o motor de salto, os parâmetros de colisão ou o cenário-base.

Foi também guardada corretamente a configuração de chegada do prefab do personagem: o ponto antigo `(60,17,90)` ainda reaparecia ao reiniciar o teste. O motor do mundo agora conserva `(4,0.24,6)`, a câmara e o limite de queda já previsto (-100), usando `PrefabUtility.RecordPrefabInstancePropertyModifications`. Isto corrige a persistência da configuração, sem resolver ou reavaliar os bugs de colisão/salto relatados pelo criador.

## Verificação

- Região 1: caminhada desde a chegada até à porta, contornando as bancas, sem saltar; E abriu a folha e levou ao asteroide. O percurso está em `Porta_Regiao_1_Percurso.json`.
- Região 2: personagem colocado junto da casa para isolar o teste da porta; E abriu e regressou. Não significa que o percurso entre as regiões foi validado.
- Região 3: personagem colocado junto da casa; botão West/Quadrado de comando virtual abriu e regressou. Dispositivos físicos restaurados após o teste.
- Nos três testes: folha observada acima de 25 graus, passagem luminosa ativa, personagem no piso antes e depois, zero reposições inesperadas e sem repetição automática da viagem. A entrada do asteroide para o mundo foi exercitada durante a preparação dos testes das regiões distantes.

Relatórios: `Porta_Regiao_1_Teste.json`, `Porta_Regiao_2_Teste.json`, `Porta_Regiao_3_Teste.json` e `Portas_Regresso_Construcao.json`. Capturas reais: `Assets/_FirstJourney/Review/Porta_Asteroide_Casa.png` e `Porta_Asteroide_Aberta.png`.

Autoria: `AgentScripts/ReturnDoorSetup.cs` (`Build`, `PersistArrival`, `RefineOpening`), revisão visual em `ReturnDoorSurvey.cs`, testes/capturas em `ReturnDoorPlaytest.cs`. `Build` não deve ser repetido sobre portas existentes. Cenas e assets guardados localmente; sem novo envio ao GitHub.
