# Encerramento — 16 outubro 2026

Projeto First Journey guardado pelo plugin oficial Unity CLI/Pipeline. Entrada: `Assets/_FirstJourney/Scenes/FirstJourney_EspacoExterior.unity`. As cenas de sessão, base e asteroide ficam guardadas fora de Play.

## Estado jogável

- Piloto aventureiro Sidekick v02 provisório, com Avatar Humanoid e 17 clips Mixamo, no lugar do AJ.
- Base original com casa, árvores, ilha do avião e ponte; asteroide de destino no mesmo exterior, a 180 m entre centros.
- Avião original com embarque, pose de piloto, voo arcade, sustentação/descolagem assistidas, aproximação final guiada e desembarque seguro nas duas luas.
- Porta do asteroide leva ao Fantasy Kingdom com três regiões no mesmo mundo. Uma casa por região permite regressar ao asteroide; o avião conserva a posição durante a sessão.
- Arte original preservada: 584 renderers da base e 38 do avião. Cenas standalone anteriores preservadas como referências.

## Regra obrigatória adotada

**Identificar → reproduzir → teste automatizado a falhar → corrigir → teste a passar → regressão.** Manter os testes de bugs corrigidos para as alterações seguintes. Decisão explícita do criador, guardada em `AGENTS.md`, `Docs/Testes_Automatizados.md`, `Docs/Bugs.md`, Notion e nota de memória.

Comando repetível: `pwsh -File Tools/Test-FirstJourney.ps1`. A suite passou na execução registada em `Docs/Regressao_Ultima_Execucao.json`: guardar projeto, 17 clips, arranque isolado, preparação de Play, avanço de frames, viagem completa, três portas e integridade final.

O primeiro ensaio do runner revelou FJ-011: sem foco e sem execução em segundo plano, os frames não avançavam. O teste isolado confirmou a falha antes da correção; a preparação foi corrigida e o mesmo teste passou na suite. Os registos antes/depois estão conservados. Os dispositivos de input foram restaurados e o Editor ficou fora de Play.

## Para retomar

- Validar manualmente a sensação do voo e o DualSense físico com o criador.
- Reproduzir FJ-001 (afundamento ao saltar/colidir) e FJ-002 (subida/salto indevido em elevações), escrever os testes específicos antes de corrigir. Continuam pendentes.
- Acabamento do personagem, percursos/colisões restantes do reino, horizonte/água e otimização.
- Picareta/mineração, inventário, cachorro/cuidado funcional e investigação separada de gravidade radial.
- Build independente e execução de Unity no CI continuam pendentes. A suite atual é local, pelo Editor e Pipeline; o comando automático usa dispositivos virtuais.

## Conteúdo do backup

Assets importados, prefabs, cenas, scripts, configurações Unity, pacotes, documentação, testes executáveis e evidências da sessão. Binários grandes são geridos pelo Git LFS. Excluídos caches e ficheiros gerados (`Library`, `Temp`, `Logs`, `UserSettings`, `TestResults`, instaladores em `Assets/DownloadCache`); os assets já importados estão incluídos. Fontes de autoria Mixar exteriores ao projeto permanecem na pasta original `C:/Ernesto/unity/assets`.

Antes do envio: `python Tools/Verify-Repository.py`, inspeção do stage e `git lfs fsck`. Após o envio, confirmar igualdade entre HEAD e `refs/heads/main` remota. O commit confirmado é indicado no Notion e na mensagem de encerramento; este ficheiro integra o próprio snapshot.
