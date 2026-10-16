# First Journey

**Cena inicial atual:** `Assets/_FirstJourney/Scenes/FirstJourney_EspacoExterior.unity`. Carrega a base e o asteroide de destino no mesmo espaço, a 180 m entre centros. **Já é possível entrar no avião, pilotar até ao asteroide, aterrar e sair.** O Fantasy Kingdom, com os três reinos, carrega à parte pela porta; o regresso conserva o personagem e o avião estacionado. Ver [controlos e testes do voo](Docs/Aviao_Voo_Arcade.md) e [organização do exterior](Docs/Espaco_Exterior_Compartilhado.md).

**Primeiro mundo ligado:** o Fantasy Kingdom contém **três reinos/regiões no mesmo mapa**, em `Assets/_FirstJourney/Scenes/FirstJourney_Reino01_FantasyKingdom.unity`. Na casa de `FirstJourney_Asteroide01.unity`, usar **E / Quadrado** para entrar no mundo. Para regressar, procurar a casa com placa **ASTEROIDE**: a porta fica na lateral, ao lado da banca com toldo vermelho. Há uma casa de regresso em cada região, com porta visível e animada. Ver [portas de regresso e testes](Docs/Portas_Regresso_Asteroide.md) e [integração do mundo](Docs/Reino01_FantasyKingdom.md).

Protótipo Unity de uma aventura em terceira pessoa: um piloto viaja entre pequenas luas num avião retro para encontrar a cura para o seu cachorro.

## Estado em 16 outubro 2026

A primeira cena contém a lua-base com a casa e árvores originais, uma ilha para o avião, uma ponte de madeira e o **Piloto aventureiro v02 como herói jogável**, já no lugar do AJ. O personagem tem movimento relativo à câmara, corrida, salto, câmara orbitável e suporte a teclado/rato e comando PS5 (DualSense).

O criador decidiu adotar o **Synty Sidekick Character Creator para criar os personagens** e rejeitou a roupa de guerreiro da primeira versão. A v02 usa a base humana Sidekick com casaco curto, roupa simples e óculos de aviador personalizados na Unity. O FBX Humanoid e o prefab próprios reaproveitam os 17 clips Mixamo. Ver [visual atual e autoria](Docs/Sidekick_Piloto_v02.md), [integração anterior](Docs/Sidekick_Integracao_v01.md) e [decisão de personagens](Docs/Personagens_Sidekick.md).

O avião original permite embarque, voo arcade com sustentação e descolagem assistidas, aproximação final guiada e desembarque seguro. O piloto reutiliza a pose `PilotIdle` do Mixamo e mantém o rig Sidekick. Mineração, inventário e cuidado do cachorro continuam por implementar. As duas ilhas da base e o asteroide pertencem ao exterior partilhado; a porta do asteroide leva ao mundo Fantasy Kingdom.

## Abrir e experimentar

1. Clonar este repositório com Git LFS instalado e executar `git lfs pull`.
2. Abrir a pasta do projeto no Unity Hub com **Unity 6000.3.23f1**.
3. Abrir `Assets/_FirstJourney/Scenes/FirstJourney_EspacoExterior.unity`.
4. Carregar em Play e clicar na janela Game.

| Ação | Teclado / rato | DualSense |
| --- | --- | --- |
| Mover | WASD ou setas | Analógico esquerdo |
| Correr | Shift | R2 |
| Saltar | Espaço | X |
| Rodar a câmara | Botão direito mantido | Analógico direito |
| Distância da câmara | Roda do rato | L1 afasta / R1 aproxima |
| Libertar cursor | Escape | Options |

No avião: **E / Quadrado** para entrar ou sair em terra; **W / R2** acelera, **S / L2** trava, **A-D / analógico esquerdo horizontal** vira, **Espaço / X ou analógico para cima** sobe e **Ctrl / analógico para baixo** desce. A menos de 19 m do estacionamento, abrandar até 25 km/h e usar **G / Círculo** para a aterragem assistida. [Guia completo](Docs/Aviao_Voo_Arcade.md).

Render pipeline: URP 17.3.0. Input System: 1.20.0. O plugin oficial Unity usa o Pipeline 0.8.0-exp.1 para controlar o Editor. Os pacotes estão fixados em `Packages/manifest.json` e `packages-lock.json`.

## Organização

- `Assets/_FirstJourney/Art`: modelos, materiais e texturas importados.
- `Assets/_FirstJourney/Scenes`: cena principal e versões anteriores preservadas.
- `Assets/_FirstJourney/Prefabs`: lua-base, ilha do avião, avião, ponte e personagem Sidekick.
- `Assets/_FirstJourney/Animation`: Animator reutilizado com 17 clips Humanoid; nomes AJ são históricos.
- `Assets/_FirstJourney/Scripts`: personagem, câmaras, avião, portas e sessão de mundos.
- `Assets/_FirstJourney/Review`: capturas feitas na Unity.
- `AgentScripts`: montagem, importação e verificações executadas pelo Pipeline; ficam fora de Assets e não entram no jogo.
- `Docs`: documentação de integração e resultados dos testes.

## Documentação

- [Avião: embarque, controlos de voo e aterragem](Docs/Aviao_Voo_Arcade.md).
- [Piloto aventureiro v02: visual atual, autoria e testes](Docs/Sidekick_Piloto_v02.md).
- [Piloto Sidekick v01: integração histórica](Docs/Sidekick_Integracao_v01.md).
- [Personagens Sidekick: decisão atual e animações](Docs/Personagens_Sidekick.md).
- [Estado da sessão e próximos passos](Docs/FimDeSessao_2026-10-16.md).
- [Integração do AJ e controlos](Docs/HeroAJ_Integracao.md).
- [Lua-base](Docs/LuaBase_Importacao.md), [ilha do avião](Docs/IlhaDoAviao.md) e [ponte](Docs/PonteEntreIlhas.md).
- [Handoff oficial no Notion](https://app.notion.com/p/3f722db8ac2381e0b277f6d24fe66f52).
- [GDD no Notion](https://app.notion.com/p/3f522db8ac2381a98f74f862e1e353e2).

## Verificação

**Regra de desenvolvimento:** identificar e reproduzir cada bug, criar um teste automatizado que falhe, corrigir e executar a regressão antes de integrar. Ver [regras do projeto](AGENTS.md), [registo de bugs](Docs/Bugs.md) e [suite automatizada](Docs/Testes_Automatizados.md). Com a Unity aberta e Pipeline ativo: `pwsh -File Tools/Test-FirstJourney.ps1`.

Voo: [relatório da ida e volta](Docs/Aviao_Playtest.json), cerca de 397 m pilotados, teclado virtual na ida e comando virtual na volta, com aterragem e desembarque nas duas luas. O avião permanece estacionado durante a visita ao reino. Zero reposições inesperadas; embarque remoto, saída em voo, saída sem chão e colisão com obstáculo de teste verificados. A aproximação à porta de regresso dentro do reino foi colocada por código; os trechos exteriores foram percorridos. [Inspeção final](Docs/Aviao_Validacao.json): cenas guardadas e zero erros de Console.

Testes no Editor confirmaram a travessia de ida e regresso com o Sidekick, animação durante a corrida, salto e aterragem. Os 17 clips foram avaliados no seu Avatar final. Os controlos foram verificados com dispositivos temporários do Input System; o teste automático do comando usa input virtual. O DualSense físico foi reconhecido na sessão anterior, mas não estava ligado no teste de 16/10.

Relatórios da v02: `Docs/Sidekick_v02_Final_Validation.json`, `Sidekick_v02_Animations_Test.json` e `Sidekick_v02_Playtest.json`. Os testes `Sidekick_Keyboard_Test.json` e `Sidekick_Gamepad_Test.json` pertencem à integração v01, cujos controlos foram preservados. Os relatórios `HeroAJ_*` registam a integração anterior e a importação das animações reaproveitadas.

Esta entrega foi verificada em Play Mode. Ainda não foi produzida uma build independente; o perfil inicial de build mantém SampleScene e deverá ser ajustado ao preparar uma build.

Os modelos importados e respetivas texturas estão no repositório. Os ficheiros de autoria Mixar continuam fora deste projeto, em `C:/Ernesto/unity/assets`, preservados sem alterações. `Library`, `Temp`, logs e configurações pessoais são ignorados pelo Git.

