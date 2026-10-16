# Piloto Sidekick jogável — 16 outubro 2026

**Registo histórico v01:** o criador rejeitou a roupa com aspeto de guerreiro. O herói ativo usa agora o piloto aventureiro v02, sem armadura. Consultar `Sidekick_Piloto_v02.md` para o estado atual, autoria e testes.

## Estado atual

O AJ foi substituído na cena principal pelo **Piloto Sidekick v01**, criado com o Synty Sidekick 1.2.7 através do plugin oficial Unity CLI/Pipeline. O criador escolheu um visual diferente do avatar de referência. Esta primeira versão usa cabelo curto, roupa civil sci-fi em tons ferrugem, azul-esverdeado e areia, sem capacete, mochila ou ferramenta acoplada.

São 23 peças e 13 406 vértices na combinação produzida. O personagem tem Avatar Humanoid próprio, altura normalizada para 1,72 m e um único controlador ativo. Movimento, câmara, teclado/rato e mapeamento de DualSense foram preservados. O cenário original mantém os seus 584 renderers.

## Abrir e editar

- Cena: `Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity`.
- Personagem na Hierarchy: `HeroSidekick`.
- Prefab jogável: `Assets/_FirstJourney/Prefabs/HeroSidekick.prefab`.
- Visual de jogo: `Assets/_FirstJourney/Prefabs/PilotoSidekick_Visual_Jogo_v01.prefab`.
- Modelo FBX e Avatar próprios: `Assets/_FirstJourney/Art/Characters/PilotoSidekick/Meshes/PilotoSidekick_v01.fbx`.
- Definição editável para carregar no Sidekick: `Assets/_FirstJourney/Art/Characters/PilotoSidekick/Definition/FirstJourney_Piloto_v01.sk`.
- Materiais e seis mapas próprios: pastas `Materials` e `Textures` de `PilotoSidekick`.
- Montagem e exportação: `AgentScripts/SidekickPilotSetup.cs`.

Carregar em Play e clicar em Game. WASD/setas movem; Shift corre; Espaço salta; botão direito roda a câmara e a roda muda a distância. No DualSense, usar analógicos, X, R2 e L1/R1, respetivamente. Escape ou Options libertam o cursor.

O ficheiro `AJ_Hero.controller` e os nomes `AJ_*` dos clips são nomes técnicos históricos. O herói ativo usa a malha e o Avatar do Sidekick.

## Exportação e visibilidade

A malha combinada guardada como asset procedural apresentou invisibilidade no renderer animado, apesar de a avaliação dos ossos e a malha obtida por BakeMesh estarem corretas. A versão final foi exportada pelo método de exportação FBX do próprio Sidekick, importada como Humanoid e ligada ao seu Avatar próprio. A sua visibilidade foi confirmada numa captura real de Game View.

Os assets procedurais e o prefab `PilotoSidekick_Visual_v01.prefab` são intermediários de autoria; usar os prefabs de jogo indicados acima. Não são necessários componentes de correção de malha por frame, mudanças globais de gráficos ou alterações aos scripts da Synty.

## Testes da versão FBX final

- 17 clips Mixamo Humanoid a 60 FPS avaliados no rig final: cinco amostras de deformação por clip; Avatar válido e geometria finita.
- Percurso de ida e regresso pela ponte com o novo personagem: **618 amostras com contacto no piso**, sem quedas nem respawns.
- Corrida a 3,80 m/s; salto com subida de aproximadamente 1,09 m, passando pelos estados Jump, Fall e Land.
- Teclado temporário: W + Shift, movimento na direção da câmara, aproximadamente 1,36 m.
- Comando virtual: movimento, corrida, salto, rotação da câmara e zoom passaram. Nenhum comando físico estava ligado durante este teste de 16/10.
- Captura real: `Assets/_FirstJourney/Review/Sidekick_GameView_v01.png`.
- Inspeção final: um motor ativo, zero materiais ausentes, câmara ligada ao novo herói, cena guardada e zero erros atuais na Console. Permanece um aviso anterior da conta Unity AI.

Relatórios: `Sidekick_GameAsset.json`, `Sidekick_Animations_Test.json`, `Sidekick_Playtest.json`, `Sidekick_Keyboard_Test.json`, `Sidekick_Gamepad_Test.json` e `Sidekick_Final_Validation.json`, em `Docs`.

## Preservação e limites

A cena anterior com AJ foi guardada em `Assets/_FirstJourney/Scenes/Versions/FirstJourney_LuaBase_AntesSidekick.unity`. O prefab, modelo e animações originais do AJ permanecem no projeto. A sua malha já não está ativa na cena principal.

A avaliação dos 17 clips verifica o retargeting e a deformação; os encaixes de picareta, cockpit e cachorro serão ajustados quando essas mecânicas forem implementadas. Voo, mineração, inventário e cuidado do cachorro continuam pendentes. As expressões faciais do Sidekick não foram integradas num sistema de diálogo.

As alterações desta sessão foram guardadas no projeto local. O último envio ao GitHub continua a ser o checkpoint de 13/10; a instalação Sidekick e esta migração ainda não foram publicadas.

