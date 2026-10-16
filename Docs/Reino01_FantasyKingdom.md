# Primeiro mundo — Fantasy Kingdom, com três reinos

16 outubro 2026. Importado e integrado através do plugin oficial Unity CLI/Pipeline.

**Organização atual:** a entrada do jogo é `Scenes/FirstJourney_EspacoExterior.unity`. O mundo é carregado pela cópia `Scenes/Worlds/FirstJourney_FantasyKingdom_Conteudo.unity`, sem personagem ou câmara duplicados. A cena `FirstJourney_Reino01_FantasyKingdom.unity` descrita na integração inicial permanece como versão standalone de referência. Ver [estrutura do exterior e conservação do avião](Espaco_Exterior_Compartilhado.md).

## Estado

Fantasy Kingdom | 3D Sample Project 1.1.0 foi importado do pacote descarregado pelo criador. A cena original `Assets/Scenes/Demo.unity` permanece como fonte. A cópia jogável está em `Assets/_FirstJourney/Scenes/FirstJourney_Reino01_FantasyKingdom.unity`, com o piloto Sidekick aventureiro v02 provisório, o Animator existente e os controlos habituais.

O exterior continua pequeno e com uma casa. A porta do asteroide abre e conduz ao **mundo Fantasy Kingdom**, que contém os três reinos/regiões `Composition_A`, `Composition_B` e `Composition_C` na mesma cena. Não são três mundos de destino separados. Não foram criados portais entre essas regiões. O avião não foi movido para dentro do mundo.

**Correção posterior à primeira integração:** o ponto inicial de regresso estava invisível junto de uma banca; não correspondia à porta real da casa. Foi substituído pela porta lateral assinalada **ASTEROIDE**, junto da banca com toldo vermelho, em cada uma das três regiões. As folhas abrem, mostram a passagem luminosa e levam ao asteroide. Ver [localização e testes das portas](Portas_Regresso_Asteroide.md).

## Experimentar

1. Abrir `Assets/_FirstJourney/Scenes/FirstJourney_Asteroide01.unity` e carregar em Play.
2. Aproximar o piloto da porta da casa e premir **E** ou **Quadrado** no DualSense (`buttonWest`; X no esquema Xbox).
3. Explorar o reino com os controlos existentes. O ponto de chegada é `(4, 0.24, 6)`; o piso coloca os pés perto de Y=0.173.
4. Para regressar, contornar a banca com toldo vermelho e aproximar-se da porta lateral da casa, com placa **ASTEROIDE**. Na primeira região, a porta está em `(5.56, 1.08, 17.2)` e pode ser usada do chão diante dos degraus. Usar **E / Quadrado**; o piloto reaparece diante da casa no asteroide. O ponto antigo `(4, 0.24, 10)` foi retirado.

Também se pode abrir diretamente a cena do reino. As duas cenas foram acrescentadas à lista de cenas do Editor para permitir `LoadSceneAsync`; não foi produzida uma build independente. A entrada precisa de uma nova pressão do botão; a chegada não aciona automaticamente a viagem de volta.

## Integração

- Importação de 1 043 caminhos de recursos, preservando os GUIDs e o conteúdo dos assets. Excluídos `ProjectSettings`, o manifesto de packages do exemplo, `VerifyProject`/a sua definição e o editor de readme que altera o layout automaticamente. O script `VerifyProject` do exemplo alteraria o pipeline e o input ao abrir cenas.
- Dependências acrescentadas pelo Package Manager: Cinemachine 3.1.7 e Visual Effect Graph 17.3.0. URP 17.3.0, Input System e Pipeline existentes preservados. O VFXManager foi atualizado automaticamente pela instalação do VFX Graph.
- A referência `UnityEditor` do controlador RTS foi protegida com `UNITY_EDITOR`, pois é usada pelos seus gizmos. Removido o namespace não usado `NUnit.Framework` do painel estatístico. Assim os scripts de demonstração não introduzem estas referências no código de runtime. Os assets e a cena fonte não foram reescritos.
- O importador nativo exige um nome TAR no cabeçalho GZIP. As primeiras cópias filtradas com nome interno `.unitypackage` não importaram recursos; a cópia final preservou os cabeçalhos TAR e usou `archtemp.tar`. O pacote original no cache não foi alterado. Houve mensagens transitórias e uma janela de importação completa, cancelada pelo criador; nenhuma configuração do exemplo foi aplicada por essa janela.
- Na cópia jogável, câmara RTS, controlador de nevoeiro/qualidade e painel estatístico do exemplo estão desativados. Há uma única câmara e um único motor do herói ativos.
- Terreno com colisor ativado. Os 228 colisores Box do exemplo cobriam volumes inteiros de edifícios e faziam o piloto parecer suspenso no ar; foram desativados na cópia. Acrescentados colisores de malha para arquitetura, pedras e objetos; a decoração de ervas e pedras soltas de `Village_Scatter` fica sem colisão. A validação conta 1 640 colisores Mesh ativos incluindo o plano subaquático original.
- Luz e atmosfera ajustadas numa configuração própria do reino, sem substituir o pipeline global. Materiais do mapa e arte original preservados.
- `FJWorldDoor.cs` trata proximidade, E/Quadrado, marcador da casa, abertura das folhas, transição e chegada. As portas das três casas reutilizam as peças originais `ANG_Porta_*`, colocadas sobre as entradas existentes, sem editar os meshes do pacote. Uma superfície luminosa cobre a porta fundida durante a abertura; não foi criado um interior de casa.
- `FJHeroMotor.fallResetHeight` torna o limite de queda configurável: -100 no reino, cujo terreno tem cotas negativas; -6 continua a ser o valor por omissão nas cenas existentes.
- Cena da lua-base preservada byte a byte: SHA-256 `31E01E8E14F302A09DEC7C200E9449FF99851D02BC3D21E70389B2C2D0B8758F`.

## Verificação

`Docs/FantasyKingdom_Playtest.json` documenta o teste inicial do ponto invisível e é histórico quanto à localização do regresso. A validação atual está em `Docs/Porta_Regiao_1_Teste.json`, `Porta_Regiao_2_Teste.json` e `Porta_Regiao_3_Teste.json`: três portas de casas funcionais, abertura observada e regresso ao asteroide pelo input real do jogo, com teclado e comando virtual. Na primeira região foi percorrido o caminho desde a chegada; nas duas regiões distantes o teste colocou o personagem junto da casa antes de interagir. Os dispositivos físicos foram isolados e restaurados; não foi feita validação manual do comando físico.

`Docs/FantasyKingdom_Validacao.json` confirma Avatar Humanoid válido, um motor e uma câmara ativos, zero scripts ausentes, zero materiais ausentes nos renderers ativos, shaders suportados e compilação sem falha. O exemplo inclui 16 renderers de pedras inativos com material vazio; foram preservados inativos. A Console foi limpa das mensagens de importação resolvidas antes da inspeção final, que terminou sem erros ou avisos.

Captura real da chegada: `Assets/_FirstJourney/Review/FantasyKingdom_GameView.png`. A captura geral está em `FantasyKingdom_VistaGeral.png`; o horizonte/água distante e o acabamento da iluminação ainda precisam de revisão visual. A zona de chegada e a transição foram verificadas; todos os caminhos, escadas, margens de água e limites do mapa ainda não foram percorridos. Não há certificação de desempenho ou build independente nesta entrega.

Fontes e automação: `AgentScripts/FantasyKingdomImport.cs`, `FantasyKingdomSetup.cs`, `FantasyKingdomSurvey.cs`, `FantasyKingdomPlaytest.cs` e `FantasyKingdomReview.cs`. Não executar novamente métodos de construção sem verificar as cenas existentes.

Trabalho guardado localmente. Sem novo envio ao GitHub nesta sessão. Voo, embarque, mineração, inventário, NPCs/missões e cuidado do cachorro continuam pendentes.
