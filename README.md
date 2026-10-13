# First Journey

Protótipo Unity de uma aventura em terceira pessoa: um piloto viaja entre pequenas luas num avião retro para encontrar a cura para o seu cachorro.

## Estado em 13 outubro 2026

A primeira cena contém a lua-base com a casa e árvores originais, uma ilha para o avião, uma ponte de madeira e o AJ do Mixamo como herói jogável. O personagem tem movimento relativo à câmara, corrida, salto, câmara orbitável e suporte a teclado/rato e comando PS5 (DualSense).

O avião está estacionado. Voo, embarque, mineração, inventário e cuidado do cachorro ainda não foram implementados. Os clips dessas interações estão preparados no Animator. As duas ilhas pertencem ao cenário-base; a segunda lua de destino ainda não está instalada.

## Abrir e experimentar

1. Clonar este repositório com Git LFS instalado e executar `git lfs pull`.
2. Abrir a pasta do projeto no Unity Hub com **Unity 6000.3.23f1**.
3. Abrir `Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity`.
4. Carregar em Play e clicar na janela Game.

| Ação | Teclado / rato | DualSense |
| --- | --- | --- |
| Mover | WASD ou setas | Analógico esquerdo |
| Correr | Shift | R2 |
| Saltar | Espaço | X |
| Rodar a câmara | Botão direito mantido | Analógico direito |
| Distância da câmara | Roda do rato | L1 afasta / R1 aproxima |
| Libertar cursor | Escape | Options |

Render pipeline: URP 17.3.0. Input System: 1.20.0. O plugin oficial Unity usa o Pipeline 0.8.0-exp.1 para controlar o Editor. Os pacotes estão fixados em `Packages/manifest.json` e `packages-lock.json`.

## Organização

- `Assets/_FirstJourney/Art`: modelos, materiais e texturas importados.
- `Assets/_FirstJourney/Scenes`: cena principal e versões anteriores preservadas.
- `Assets/_FirstJourney/Prefabs`: lua-base, ilha do avião, avião, ponte e AJ.
- `Assets/_FirstJourney/Animation`: Animator do AJ com 17 clips Humanoid.
- `Assets/_FirstJourney/Scripts`: movimento do AJ e câmara.
- `Assets/_FirstJourney/Review`: capturas feitas na Unity.
- `AgentScripts`: montagem, importação e verificações executadas pelo Pipeline; ficam fora de Assets e não entram no jogo.
- `Docs`: documentação de integração e resultados dos testes.

## Documentação

- [Estado da sessão e próximos passos](Docs/FimDeSessao_2026-10-13.md).
- [Integração do AJ e controlos](Docs/HeroAJ_Integracao.md).
- [Lua-base](Docs/LuaBase_Importacao.md), [ilha do avião](Docs/IlhaDoAviao.md) e [ponte](Docs/PonteEntreIlhas.md).
- [Handoff oficial no Notion](https://app.notion.com/p/3f722db8ac2381e0b277f6d24fe66f52).
- [GDD no Notion](https://app.notion.com/p/3f522db8ac2381a98f74f862e1e353e2).

## Verificação

Testes no Editor confirmaram a travessia de ida e regresso com o AJ, animação durante a corrida, salto e aterragem. Os controlos de teclado e de comando foram verificados com dispositivos temporários do Input System. O DualSense físico foi reconhecido e restaurado após os testes; o teste automático do comando usa input virtual.

Relatórios atuais: `Docs/HeroAJ_Final_Validation.json`, `HeroAJ_Playtest.json`, `HeroAJ_Keyboard_Test.json` e `HeroAJ_Gamepad_Test.json`. Os relatórios `HeroAJ_*_Import.json` registam a etapa inicial de importação; o estado Humanoid final está em `HeroAJ_Humanoid.json`.

Esta entrega foi verificada em Play Mode. Ainda não foi produzida uma build independente; o perfil inicial de build mantém SampleScene e deverá ser ajustado ao preparar uma build.

Os modelos importados e respetivas texturas estão no repositório. Os ficheiros de autoria Mixar continuam fora deste projeto, em `C:/Ernesto/unity/assets`, preservados sem alterações. `Library`, `Temp`, logs e configurações pessoais são ignorados pelo Git.

