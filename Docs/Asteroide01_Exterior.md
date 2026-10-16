# Primeiro asteroide de destino — exterior v01

16 outubro 2026. Construído e guardado através do plugin oficial Unity CLI/Pipeline.

## Direção e estado

O criador confirmou a referência à série animada **O Pequeno Príncipe (2010)**: pequenos asteroides com uma casa, cuja porta dá acesso a um reino maior. Escolheu **Fantasy Kingdom** para o primeiro reino e informou que tem aprovação para o usar. Esta nota regista a decisão e a informação do criador, sem substituir os documentos de autorização.

O exterior está criado numa cena separada. É uma rocha arredondada e facetada, com **16,22 m de largura e 10,25 m de altura**, uma pequena superfície superior caminhável e apenas uma casa. A casa reutiliza 86 peças da arte original do projeto; não é uma construção extraída do Fantasy Kingdom. Não foram colocadas árvores, vegetação decorativa, outras casas ou uma povoação na superfície.

O piloto aventureiro v02 permanece provisório, autorizado pelo criador para os testes. O acabamento do personagem continua por rever.

## Abrir e testar

- Cena: `Assets/_FirstJourney/Scenes/FirstJourney_Asteroide01.unity`.
- Exterior reutilizável: `Assets/_FirstJourney/Prefabs/Asteroide01_Exterior.prefab`.
- Malha, materiais, céu e estrelas: `Assets/_FirstJourney/Art/Worlds/Asteroide01`.
- Captura real de Game View: `Assets/_FirstJourney/Review/Asteroide01_GameView.png`.
- Construção: `AgentScripts/FirstDestinationAsteroid.cs`.
- Verificação e captura: `AgentScripts/FirstAsteroidReview.cs`.

Abrir a cena e carregar em Play. O piloto usa os controlos existentes de movimento, corrida, salto e câmara. `Main Camera` é a única câmara ativa; `Vista_Chegada_Asteroide01` é uma câmara desativada para rever o conjunto.

Para gerar a imagem, executar `FirstAsteroidReview.CaptureGameOverview` em Play Mode. O método espera frames reais para estabilizar a câmara e a malha animada, capta Game View e restaura a câmara e os controlos. Uma primeira captura por `Camera.Render` mostrou conteúdo antigo da cena anterior; foi descartada. A captura real e a hierarquia da cena confirmaram o exterior isolado.

## Porta e pontos de ligação

- `Porta_Reino_FantasyKingdom`: folha agrupada num pivô próprio, com colisor de porta fechada.
- `Entrada_Reino_FantasyKingdom`: ponto reservado para a passagem ao reino.
- `Regresso_Do_Reino`: ponto de reaparecimento no exterior, diante da casa.
- `Chegada_Do_Aviao_Reserva`: referência de chegada para o trabalho futuro de voo.

**Atualização de 16/10: a transição está implementada.** A porta abre com E / Quadrado e conduz à cópia jogável do Fantasy Kingdom; o regresso coloca o piloto diante da casa. Ver [integração do reino e teste de ida e volta](Reino01_FantasyKingdom.md). A validação abaixo documenta a construção inicial do exterior; o estado da ligação está em `FantasyKingdom_Playtest.json`. Não foram implementados voo, aterragem física, embarque/desembarque ou gravidade radial. O piloto caminha na parte superior com a gravidade vertical existente.

## Verificação executada no Editor

- Percurso à volta da casa e aproximação à porta: **482 de 482 amostras com contacto no piso**, zero reposições inesperadas.
- Corrida: 3,80 m/s; salto com subida de 1,04 m e estados Jump, Fall e Land observados.
- Avatar Humanoid válido, um motor ativo, uma câmara ativa, zero materiais ausentes e zero erros atuais na Console na inspeção final.
- Permanecia um aviso na Console; sem falha de compilação. Nenhuma configuração global de gráficos foi alterada para as capturas.
- Cena e prefab guardados. Não foi gerada ou testada uma build independente.

Relatórios: `Docs/Asteroide01_Construcao.json`, `Asteroide01_Playtest.json` e `Asteroide01_Validacao.json`.

## Preservação e continuação

A lua-base, a ilha do avião, a ponte e a arte original foram preservadas. O SHA-256 do ficheiro `FirstJourney_LuaBase.unity` antes e depois desta montagem permaneceu **31E01E8E14F302A09DEC7C200E9449FF99851D02BC3D21E70389B2C2D0B8758F**. Os assets originais da casa foram referenciados, sem edição das suas malhas ou materiais.

O próximo passo é preparar o reino com Fantasy Kingdom e implementar a travessia da porta e o regresso ao exterior. A construção do exterior não significa que o reino ou a ligação já funcionem. O trabalho está guardado localmente; não houve envio ao GitHub nesta etapa.
