# Personagens — Sidekick, atualizado em 16 outubro 2026

**Estado atual:** o Piloto aventureiro v02 está na cena principal. O criador rejeitou o aspeto de guerreiro da v01; foram retiradas as proteções e a túnica e criada roupa simples de aviador sobre a base e o rig do Sidekick. Os 17 clips e a travessia com salto passaram na v02. Ver `Sidekick_Piloto_v02.md` para os assets e limites de edição da roupa personalizada. A integração v01 e as secções seguintes são registos históricos.

## Direção aprovada pelo criador

O **Synty Sidekick Character Creator passa a ser a ferramenta adotada para criar os personagens humanos do First Journey**. Os novos personagens Sidekick vão substituir o AJ. A escolha da ferramenta e a substituição estão decididas; a construção e instalação do novo protagonista ficam para a próxima sessão.

A sessão de hoje serviu para validar o acesso do Codex à ferramenta através do plugin oficial Unity. Esse acesso foi comprovado por um teste de criação, proporções, expressão facial e animação num modelo temporário. Não foi realizada a substituição na cena. O AJ permanece instalado apenas como implementação provisória até à migração.

## Animações incluídas e reaproveitamento

A instalação contém animações e poses de rosto, como piscar, felicidade, tristeza, preocupação e raiva, ciclos faciais e uma animação corporal de repouso `A_Body_IdleSubtle` nas demos. A inspeção no Editor identificou 58 assets de clip, incluindo poses e versões provenientes de FBX; este número não representa 58 ações distintas de gameplay.

Para a locomoção e as ações do jogo, vamos aproveitar os **17 clips Humanoid do Mixamo já existentes**: repouso, caminhada, corrida, salto, queda, aterragem e as ações preparadas de ferramenta, recolha, avião e cuidado do cachorro.

Os personagens Sidekick usam Avatar Humanoid, permitindo aplicar essas animações ao novo esqueleto por retargeting. No teste já realizado, o rig Sidekick executou a corrida do `AJ_Hero.controller`, com movimento observado da perna. Os restantes clips ainda precisam de validação durante a migração. A referência ao nome AJ nos ficheiros atuais é histórica e não obriga a continuar a usar a sua malha.

## Próxima sessão

1. Construir o novo protagonista no Sidekick, alinhado à direção artística do projeto.
2. Guardar a definição editável e gerar o prefab com Avatar Humanoid válido.
3. Aplicar o Animator e os clips existentes ao novo modelo; preservar movimento, câmara e controlos de teclado/rato e DualSense.
4. Validar os 17 clips, corrigindo escala, pés, mãos, contactos e transições quando necessário. Nas ações com picareta, cockpit e cachorro, ajustar a posição dos objetos às mãos e à pose.
5. Substituir o AJ na cena principal e verificar novamente percurso, ponte, corrida e salto, preservando uma versão anterior.

As expressões faciais do Sidekick podem complementar as animações corporais numa etapa posterior. A existência de clips não implementa, por si só, voo, mineração ou cuidado do cachorro.

## Evidência

- Avaliação: `Docs/Sidekick_Avaliacao.md`.
- Teste de acesso e retargeting: `Docs/Sidekick_Access_Probe.json`.
- Inventário das animações instaladas: `Docs/Sidekick_Animation_Inventory.json`.

