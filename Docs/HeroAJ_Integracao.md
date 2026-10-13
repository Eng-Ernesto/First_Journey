# First Journey — AJ jogável, versão 01

O personagem instalado é o AJ selecionado no Mixamo: boné vermelho, óculos, roupa escura e mochila original. A fonte completa é `AJ_WithSkin.fbx`, uma cópia de `Aj@Petting Animal (1).fbx` descarregada pelo utilizador. `explorador.fbx` não é usado pelo herói.

O Avatar Humanoid foi validado no Editor e durante o jogo. Os 17 clips usam esse Avatar e mantêm 60 FPS. Os materiais preservam as quatro texturas extraídas do FBX; a transparência do atlas facial foi configurada para os olhos, sobrancelhas e boca.

## Jogar

Abrir `Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity`, carregar em Play e clicar na janela Game.

- WASD ou setas: mover em relação à direção da câmara.
- Shift: correr.
- Espaço: saltar.
- Botão direito do rato, mantido: rodar a câmara.
- Roda do rato: aproximar ou afastar a câmara.
- Escape: libertar o cursor.

O comando PS5 (DualSense) também é suportado pelo Input System já instalado e foi reconhecido como `DualSenseGamepadHID` neste computador. Em Play, clicar na janela Game e usar:

- Analógico esquerdo: mover, com velocidade proporcional à inclinação.
- Analógico direito: rodar a câmara.
- X: saltar.
- R2, mantido: correr.
- L1: afastar a câmara; R1: aproximar.
- Options: libertar o cursor.

O teclado e rato continuam disponíveis. As dicas na janela Game mostram os botões do comando quando este está ligado.

A câmara segue o herói e limita a aproximação a obstáculos. Se o personagem cair fora das ilhas, regressa ao ponto inicial. O deslocamento usa CharacterController; o Animator acompanha a velocidade e os estados de salto, queda e aterragem.

## Animator e interações

O Animator contém 15 estados e os 17 clips. Locomotion combina Idle, Walking e Standard Run; Jump, Fall e Land acompanham o movimento físico. Os sete clips de repetição têm Loop Time e Loop Pose ativos. Root motion está desativado para o controlo gerir o deslocamento.

Os onze clips de recolha, ferramenta, avião e cachorro estão ligados a estados de interação, com transições para as poses seguintes. São preparação para as próximas mecânicas. Não há ainda mineração funcional, entrada no cockpit, voo controlável ou interação com um cachorro na cena. Esses estados podem ser acionados por `FJHeroMotor.PlayInteraction` e terminados por `StopInteraction` quando as mecânicas forem integradas.

## Verificação concluída

- 17 clips Humanoid, Avatar válido, quatro malhas com skin e materiais com texturas.
- AJ normalizado para 1,72 m; CharacterController com raio de 0,28 m.
- Travessia com o AJ real do caminho da casa à ilha do avião e regresso: 596 amostras, todas com contacto no piso; sem queda ou respawn.
- Animação efetiva do pé observada durante a corrida; velocidade máxima de 3,80 m/s.
- Salto com subida de 1,09 m e passagem pelos estados Jump, Fall e Land.
- Teste do Input System com teclado temporário: W + Shift, deslocamento de 1,43 m na direção da câmara e velocidade de 3,57 m/s. O dispositivo temporário foi removido e o teclado original restaurado.
- DualSense físico reconhecido e ativo. Teste automático com comando virtual: movimento e corrida (1,47 m; 3,59 m/s), rotação da câmara, zoom e salto (1,09 m); sem respawns. O comando físico foi restaurado após o teste. Os botões físicos ainda devem ser experimentados pelo utilizador.
- Os 584 renderers da arte original da lua-base foram preservados.

## Entrega

- Prefab: `Assets/_FirstJourney/Prefabs/HeroAJ.prefab`.
- Animator: `Assets/_FirstJourney/Animation/AJ_Hero.controller`.
- Modelo e texturas: `Assets/_FirstJourney/Art/Characters/HeroAJ`.
- Scripts: `Assets/_FirstJourney/Scripts/FJHeroMotor.cs` e `FJOrbitCamera.cs`.
- Construção: `AgentScripts/AJHeroSetup.cs` e `PrepareAJHero.cs`.
- Verificação: `AgentScripts/AJHeroPlaytest.cs`, `AJGamepadPlaytest.cs`, `Docs/HeroAJ_Humanoid.json`, `HeroAJ_Playtest.json`, `HeroAJ_Keyboard_Test.json`, `HeroAJ_Gamepad_Test.json` e `HeroAJ_Final_Validation.json`.
- Prévia: `Assets/_FirstJourney/Review/HeroAJ_Unity_v01.png`.
- A cena anterior está preservada em `Assets/_FirstJourney/Scenes/Versions/FirstJourney_LuaBase_AntesHeroiAJ.unity`.
