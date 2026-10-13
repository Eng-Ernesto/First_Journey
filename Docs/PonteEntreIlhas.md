# Ponte entre as ilhas — versão 01

A ponte foi construída diretamente no Editor Unity através do plugin oficial Unity CLI/Pipeline. As malhas, materiais, colisões, prefab e alterações na cena foram criados com APIs do Editor, sem modificar o ficheiro de cena à mão.

## Arte e implantação

- Ponte de madeira com 8,65 metros de comprimento e 2,38 metros de largura. A largura livre entre proteções é aproximadamente 2,06 metros.
- 26 tábuas com bordas chanfradas, leve curvatura do tabuleiro, longarinas, travessas e apoios nas duas ilhas.
- Corrimãos de madeira em ambos os lados, com doze postes e duas barras por trecho.
- Materiais `ANG_Madeira` e `ANG_MadeiraClara` reutilizados do cenário original.
- Caminho de terra de aproximadamente 1,2 metros de largura entre o caminho da casa e a ponte, contornando a árvore existente.
- As pontas do tabuleiro avançam sobre o terreno das ilhas; o piso começa apenas cinco centímetros acima do terreno para facilitar a entrada.
- A arte original da casa e das árvores permaneceu intacta: 584 renderers na lua-base.

## Colisão e verificação

O piso usa uma malha de colisão contínua, para evitar buracos entre tábuas. Dez proteções laterais com BoxCollider acompanham os corrimãos. São onze colisores ao todo.

Foram verificadas seis posições de apoio nas duas margens, 183 amostras sobre o tabuleiro e o percurso de acesso contra o terreno e obstáculos. Não há materiais ausentes ou amostras de piso falhadas.

Em Play Mode, um CharacterController temporário de 1,72 metros de altura e raio de 0,28 metros percorreu o caminho de acesso e atravessou a ponte nos dois sentidos: 906 passos, 905 contactos com o piso, sem queda ou bloqueio. As colisões laterais limitaram o deslocamento a aproximadamente 0,73 metros para ambos os lados. O objeto de teste foi removido após a verificação. A consola não apresentou erros atuais de compilação ou execução.

O AJ e os seus controlos foram instalados posteriormente. A travessia foi também verificada com o personagem real: 596 amostras com contacto no piso, ida e regresso, sem queda ou respawn. Ver `Docs/HeroAJ_Playtest.json` e `Docs/HeroAJ_Integracao.md`.

## Entrega editável

- Cena: `Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity`.
- Prefab: `Assets/_FirstJourney/Prefabs/IslandBridge.prefab`, objeto `Ponte_Entre_Ilhas`.
- Malhas: `Assets/_FirstJourney/Art/IslandBridge/Meshes`.
- Construção e verificação: `AgentScripts/IslandBridge.cs`.
- Vista geral: `Assets/_FirstJourney/Review/Ponte_DuasIlhas_Unity_v01.png`.
- Detalhe: `Assets/_FirstJourney/Review/Ponte_Detalhe_Unity_v01.png`.
- Resultados: `Docs/IslandBridge_Result.json` e `Docs/IslandBridge_Traversal.json`.
- Cena anterior preservada em `Assets/_FirstJourney/Scenes/Versions/FirstJourney_LuaBase_AntesPonte.unity`.
