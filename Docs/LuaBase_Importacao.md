# First Journey — primeira cena na Unity

Importação concluída em 13 outubro 2026 pelo plugin oficial Unity, usando Unity CLI e o Pipeline do Editor 6000.3.23f1.

## Abrir
Na janela Project, abrir Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity.
O cenário encontra-se em LuaBase_ArteOriginal; o prefab reutilizável é Assets/_FirstJourney/Prefabs/BaseMoon.prefab.
A cena recebeu posteriormente o AJ jogável, a câmara em terceira pessoa, a ilha do avião e a ponte. O estado atual está em `FimDeSessao_2026-10-13.md` e `HeroAJ_Integracao.md`; os detalhes seguintes registam a importação original.

## Conteúdo
Cenário original de casa, terreno, caminho, plantas, pedras e 20 árvores de casa do  mato.mixar.
584 renderers, 32 materiais URP e 33 colisores básicos no terreno, estrutura da casa e troncos.
As árvores eram instâncias de coleções; foram convertidas em malhas para exportação, preservando a composição.
Quatro materiais procedurais de troncos/folhagem foram convertidos para texturas de cor e normal de 2048 px.
A geometria e os materiais mantêm os nomes do modelo original.
Luzes e enquadramento foram adaptados à Unity; o aspeto da iluminação pode diferir do render do Mixar.

## Preservação
O ficheiro-fonte C:/Ernesto/unity/assets/casa do  mato.mixar foi apenas lido, nunca gravado.
SHA256: B66F88C81FF650E0A7D070EA7711659CD5832AFBA2EB964A8FCF99158F84E056.
SampleScene e os modelos do personagem foram preservados.
A alteração que já existia em Packages/packages-lock.json não foi substituída.
As configurações adicionais que o Editor atualizou durante a sessão ficaram preservadas.

## Verificações no Editor
- Compilação do script de montagem pelo Pipeline, sem diagnósticos.
- Cena e prefab gravados; 584 renderers, nenhuma referência de material em falta.
- Comparação dos materiais dos objetos com o manifesto: zero diferenças.
- Submeshes das três famílias de folhas: quatro variantes de cor, todas com índices de triângulos.
- Captura direta da Main Camera: Assets/_FirstJourney/Review/LuaBase_Unity_v01.png.
- Entrada e saída de Play Mode confirmadas; zero erros atuais na Console.
- O aviso preexistente de acesso à conta do Unity AI continua separado desta importação.
- Bootstrap de reconexão temporário removido pelo AssetDatabase.

## Limites
Os colisores foram posteriormente testados com o AJ no caminho entre as ilhas, incluindo ida e regresso pela ponte. Ainda falta a revisão manual completa das restantes áreas.
Movimento e controlos foram acrescentados na integração do AJ. Voo, mineração e cuidado do cachorro continuam pendentes.
Não houve otimização de LODs nem decisão sobre gravidade esférica.
O exportador FBX emitiu avisos sobre slots de material. A validação na Unity confirmou os 32 materiais, a correspondência por objeto e os submeshes de folhas; estes avisos ficam registados em Export_v01/conversion.log.

## Ficheiros principais
Assets/_FirstJourney/Art/BaseMoon/Models/FirstJourney_BaseMoon_v01.fbx
Assets/_FirstJourney/Art/BaseMoon/Materials/
Assets/_FirstJourney/Art/BaseMoon/textures/
Assets/_FirstJourney/Art/BaseMoon/Data/manifest.json
Assets/_FirstJourney/Prefabs/BaseMoon.prefab
Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity
AgentScripts/BaseMoonScene.cs — montagem via unity command run_script, com proteção contra substituir uma cena existente.
AgentScripts/BaseMoonReview.cs — enquadramento da câmara.
AgentScripts/BaseMoonValidate.cs — comparação dos materiais.

