# Registo de bugs — First Journey

A partir de 16 outubro 2026, o fluxo obrigatório é **identificar → reproduzir → teste automatizado que falha → corrigir → teste passa → regressão**. Ver `AGENTS.md` e `Testes_Automatizados.md`.

## FJ-001 — Afundamento durante salto/colisão

- Estado: relatado pelo criador; reprodução determinística e teste de regressão pendentes. Correção adiada expressamente durante a integração das portas.
- Sintoma: o personagem afunda ao interagir com algumas colisões durante o salto no Fantasy Kingdom.
- Esperado: a cápsula mantém separação do piso/obstáculo e o personagem termina apoiado em superfície válida.
- Observado: afundamento relatado; local exato, sequência de input e valores de penetração ainda não medidos.
- Próximo passo: identificar uma posição e sequência reproduzíveis, medir cápsula/piso e escrever um teste que falhe antes de alterar o motor. Os testes de salto em chão livre existentes não certificam este caso.

## FJ-002 — Subida/salto indevido em elevações

- Estado: relatado pelo criador; reprodução determinística e teste de regressão pendentes. Correção adiada.
- Sintoma: o personagem parece saltar/subir em qualquer elevação, além do comportamento desejado.
- Esperado: obstáculos e inclinações respeitam os limites de movimento; saltos voluntários dependem do input de salto.
- Observado: relato ainda sem elevação, ângulo, altura e sequência de input isolados. É necessário distinguir salto, step do CharacterController e resolução da colisão.
- Próximo passo: preparar um percurso determinístico com as elevações que reproduzem o caso e fixar a expectativa com o criador antes da correção.

## Comportamentos já protegidos por automação

| Caso | Sintoma a impedir | Teste executável |
| --- | --- | --- |
| FJ-003 | Não conseguir regressar ao asteroide pelas casas das três regiões | `SharedExteriorPlaytest.RoundTrips` |
| FJ-004 | Conseguir embarcar à distância | `AircraftPlaytest.Journey`: `distantBoardBlocked` |
| FJ-005 | Sair do avião em voo ou sem chão seguro | `AircraftPlaytest.Journey`: `airborneExitBlocked`, `unsafeExitBlocked` |
| FJ-006 | Avião atravessar um obstáculo do ensaio | `AircraftPlaytest.Journey`: `obstacleBlocked` |
| FJ-007 | Perder/reposicionar o avião durante a visita ao mundo | `AircraftPlaytest.Journey`: `planePersistsDuringWorldVisit` |
| FJ-008 | Estacionamento impedir o trajeto a pé até à porta | `AircraftPlaytest.Journey`: caminhada após aterrar e regresso ao avião |
| FJ-009 | Duplicar personagem, câmara ou listener nas transições | `AircraftPlaytest.Journey`, `SharedExteriorPlaytest.RoundTrips`, `AircraftReview.ValidateSaved` |
| FJ-010 | Perder Avatar, clips ou deformar a malha do piloto provisório | `YoungPilotAnimationValidation.ValidateAll` |

Os casos FJ-003 a FJ-010 têm testes existentes aprovados durante as integrações; o registo não afirma que todos tenham tido um teste a falhar escrito antes da correção. A exigência de provar a falha antes de corrigir passa a ser obrigatória a partir desta decisão. As limitações de cada teste constam do guia da suite.

## FJ-011 — Runner sem frames quando o Editor está em segundo plano

- Estado: reprodução e teste a falhar confirmados antes da correção; correção incluída no runner e teste mantido na suite.
- Reprodução: iniciar a cena exterior pelo runner com a janela Game sem foco e `Application.runInBackground=false`. O Editor entra em Play, mas os frames não avançam e a sessão não termina a inicialização.
- Esperado: a preparação da automação garante frames ativos e inicialização antes de despachar o percurso.
- Observado: a primeira execução expirou em 60 s; o teste isolado `AutomationReadinessTest.FramesAdvance` mediu frame 474 no início e no fim de 4 s, falhando especificamente por falta de avanço. Evidência: `Regressao_FalhaInicial_Runner.json` e `Regressao_FJ011_Antes.json`.
- Correção: `SessionClosure.PreparePlay` ativa execução em segundo plano, retira pausa e mostra/foca a janela Game. Só depois corre o mesmo teste e os cenários de gameplay. Não foi alterada a lógica do jogo para resolver esta falha de automação.

## FJ-012 — Dados binários grandes fora do Git LFS

- Sintoma: os assets LFS foram enviados, mas o pacote Git normal de 37,64 MiB recebeu HTTP 408 em tentativas sucessivas; a branch remota continuou no commit anterior.
- Reprodução/diagnóstico: a inspeção do índice encontrou dados binários do terreno, baking e árvores SpeedTree ainda como blobs Git normais. A verificação anterior só rejeitava blobs acima de 50 MiB, deixando passar estes ficheiros menores em conjunto.
- Esperado: assets binários acima de 1 MiB são geridos pelo LFS e o índice contém os seus ponteiros.
- Teste: `Tools/Verify-Repository.py` passa a detetar conteúdo binário nos assets grandes e exigir LFS. Registo da falha antes da correção em `Docs/Backup_FJ012_Antes.json`.
- Correção de integridade: acrescentadas as regras LFS e renormalizado o índice, preservando os bytes locais dos assets. O mesmo teste passou; resultado em `Docs/Backup_Integridade.json`. O commit anterior não publicado foi conservado na branch local `codex/backup-before-lfs-2026-10-16` antes de preparar o snapshot final.
- Âmbito: integridade e transporte do backup; não altera a geometria, materiais ou bytes dos assets usados pela Unity. O HTTP 408 é o erro observado; a relação com o tamanho/transporte é uma hipótese operacional a validar no reenvio.
