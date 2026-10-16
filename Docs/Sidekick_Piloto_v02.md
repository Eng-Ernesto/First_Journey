# Piloto aventureiro v02 — 16 outubro 2026

**Validação posterior do criador:** o visual não agradou como resultado final, mas foi autorizado para os testes. Mantém-se como personagem provisório; a direção visual final será trabalhada mais tarde.

O criador rejeitou o aspeto de guerreiro da versão v01. O protagonista ativo passou a usar uma direção de jovem piloto aventureiro: casaco curto ferrugem com gola e punhos claros, camisa clara, calças azul-escuras, botas simples e óculos de aviador na testa. Foram retiradas as placas do peito, antebraços e canelas, as luvas de combate e a túnica. As proporções ficaram mais leves, sem musculatura acentuada e com a cabeça ligeiramente maior.

## Assets e edição

- Cena: `Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity`, objeto `HeroSidekick`.
- Prefab do herói: `Assets/_FirstJourney/Prefabs/HeroSidekick.prefab`.
- Visual completo para jogo: `Assets/_FirstJourney/Prefabs/PilotoAventureiro_Visual_Jogo_v02.prefab`.
- Modelo e Avatar próprios: `Assets/_FirstJourney/Art/Characters/PilotoAventureiro/Meshes/PilotoAventureiro_v02.fbx`.
- Autoria: `AgentScripts/SidekickYoungPilotSetup.cs`, métodos `BuildVisual`, `RenderPreview` e `RefreshInstalledVisual`.
- Receita: `Assets/_FirstJourney/Art/Characters/PilotoAventureiro/Definition/PilotoAventureiro_v02_Recipe.json`.

Mantém-se o Sidekick como base humana e rig. As peças de vestuário instaladas tinham proteções integradas; por isso a nova roupa foi construída como malha própria na Unity, ligada aos ossos do Sidekick, e exportada com o modelo pelo exportador FBX da ferramenta. Não foram modificados os fontes da Synty. O visual final importado tem dois renderers, 7 857 vértices e 5 641 triângulos.

O ficheiro `Definition/FirstJourney_Piloto_v02.sk` guarda **apenas a base humana e as cores do Sidekick**. Carregá-lo no criador não reconstrói o casaco, calças, botas e óculos personalizados. O modelo vestido completo está no prefab de jogo; o script e a receita acima conservam a autoria da roupa e do ajuste de proporções. O prefab sem `Jogo` no nome é um intermediário procedural; usar o prefab de jogo na cena.

O rosto mantém o shader Sidekick; a roupa usa URP Lit opaco com a mesma paleta, evitando os artefactos que o shader de partes da Synty apresentava na geometria personalizada. Os materiais e texturas são próprios desta versão.

## Verificação

- Avatar Humanoid válido; os 17 clips Mixamo foram avaliados, com cinco amostras de deformação por clip.
- Ida e regresso pela ponte em Play Mode: **611 de 611 amostras com contacto no piso**, zero respawns, corrida a 3,80 m/s.
- Salto com subida de 1,093 m, passando por Jump, Fall e Land.
- Um motor ativo, câmara ligada ao herói, zero materiais ausentes, sem falha de compilação; os 584 renderers originais da lua-base foram preservados.
- Cena e prefab guardados. Não foram alterados os scripts de controlo. Os testes anteriores de teclado e comando virtual continuam documentados na integração v01; não foram repetidos para esta alteração de roupa.
- Imagens: `Assets/_FirstJourney/Review/PilotoAventureiro_Preview_v02.png` e `PilotoAventureiro_GameView_v02.png`.
- Relatórios: `Docs/Sidekick_v02_Animations_Test.json`, `Sidekick_v02_Playtest.json` e `Sidekick_v02_Final_Validation.json`.

A v01 permanece preservada nos seus assets e imagens como referência histórica. A direção v02 está instalada para revisão artística pelo criador; não foi registada como visual final aprovado. O trabalho foi guardado localmente, sem novo envio ao GitHub nesta alteração.
