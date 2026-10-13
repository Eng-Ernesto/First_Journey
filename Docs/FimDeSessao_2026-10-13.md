# Estado guardado — 13 outubro 2026

## Entrega atual

Projeto: `First_Journey`. Repositório: https://github.com/Eng-Ernesto/First_Journey, branch `main`.

Cena principal: `Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity`.

- Lua-base original: casa, terreno, caminho, plantas, pedras e 20 árvores; 584 renderers preservados.
- Ilha do avião: terreno e decoração derivados da arte existente, com o avião original separado do astronauta.
- Ponte de madeira criada na Unity: 8,65 m de comprimento, 2,38 m de largura e piso de colisão contínuo.
- Herói aprovado para este protótipo: **AJ do Mixamo**, sem capacete, com boné vermelho, óculos, roupa e mochila originais. O modelo `explorador.fbx` não é usado pelo herói.
- AJ: quatro malhas com skin, altura de 1,72 m, Avatar Humanoid válido e 17 clips a 60 FPS.
- Movimento relativo à câmara, corrida, salto, queda, aterragem e reposição no ponto inicial em caso de queda fora das ilhas.
- Câmara em terceira pessoa com órbita, zoom e limitação perante obstáculos.
- Controlos de teclado/rato e DualSense; ver `HeroAJ_Integracao.md` e o README.

## Evidência dos testes

| Verificação | Resultado | Relatório |
| --- | --- | --- |
| Ida e regresso entre as ilhas com AJ | 596 amostras com contacto no piso; sem respawns | `HeroAJ_Playtest.json` |
| Corrida e salto | 3,80 m/s; subida de 1,09 m; estados Jump, Fall e Land observados | `HeroAJ_Playtest.json` |
| Teclado pelo Input System | W + Shift: 1,43 m na direção da câmara, 3,57 m/s | `HeroAJ_Keyboard_Test.json` |
| Comando pelo Input System | Movimento, corrida, órbita, zoom e salto passaram; sem respawns | `HeroAJ_Gamepad_Test.json` |
| Dispositivo físico | DualSense Wireless Controller, layout DualSenseGamepadHID, ativo | Reconhecido ao vivo no Editor |
| Referências do AJ | 17 clips Humanoid, zero materiais ausentes, câmara ligada | `HeroAJ_Final_Validation.json` |

O teste automático do comando utilizou um dispositivo virtual. Os dispositivos físicos foram restaurados no fim dos testes. A verificação final da Console apresentou zero erros atuais e nenhum erro de compilação; permanece um aviso de conta Unity AI registado anteriormente.

## Fontes preservadas

- Cenário: `C:/Ernesto/unity/assets/casa do  mato.mixar`.
- Avião: `C:/Ernesto/unity/assets/autronauta.mixar`.
- AJ completo: cópia `C:/Ernesto/unity/assets/Characters/HeroAJ/Source/AJ_WithSkin.fbx`, importada no projeto como `AJ_Model.fbx`.

Os fontes Mixar ficam fora do repositório Unity. Os FBX, texturas, materiais, prefabs, animações e scripts necessários à cena estão incluídos. Git LFS já está configurado para modelos e imagens.

As cenas anteriores foram mantidas em `Assets/_FirstJourney/Scenes/Versions`. A SampleScene original permanece preservada.

## Ao retomar

1. Abrir a cena principal e experimentar os controlos do AJ, incluindo o DualSense físico.
2. Validar com o criador a próxima mecânica: interação de embarque e voo arcade ou picareta com um depósito de recurso.
3. Ligar as animações de interação às mecânicas escolhidas. Os estados existentes não representam sistemas funcionais de voo, mineração ou cuidado do cachorro.
4. Preparar a lua de destino e fechar gradualmente o loop de expedição e regresso.

Ainda pendentes: cachorro instalado e cuidado funcional, mineração/inventário, embarque/desembarque, voo controlável, segunda lua e teste separado de gravidade radial. Não alterar toda a cena para esfera sem validação.

Não voltar a executar os scripts de construção por rotina: a cena e os prefabs já estão montados. Não correr `RegisterAJDownload.ps1` para atualizar o estado final; é uma ferramenta histórica da recolha dos downloads. Para uma futura build, configurar a cena principal no perfil de build, que ainda aponta para SampleScene.

