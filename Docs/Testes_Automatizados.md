# Testes automatizados e regressão

Regra do criador em 16 outubro 2026: primeiro identificar e reproduzir os bugs; escrever testes automatizados para impedir que regressem quando o projeto mudar. A regra está em `AGENTS.md` e aplica-se às próximas correções e alterações de comportamento.

## Processo obrigatório

1. Abrir/atualizar uma entrada em `Docs/Bugs.md`, com cenário, input, esperado e observado.
2. Escrever um teste determinístico que reproduza o bug e executar: deve falhar pelo motivo esperado. Uma falha de ligação ou de compilação não prova o bug.
3. Corrigir o comportamento e executar novamente: o teste deve passar.
4. Executar os testes afetados e a suite de regressão antes de integrar. Guardar o teste para as próximas alterações e registar os resultados.

Sem reprodução confirmada, não marcar o bug como corrigido. Os problemas de afundamento e de subida indevida relatados no reino estão registados, continuam pendentes e precisam dos próprios testes de reprodução.

## Um comando para repetir a regressão

Com o projeto aberto no Unity 6000.3.23f1 e o servidor Pipeline ativo, executar na raiz:

```powershell
pwsh -File Tools/Test-FirstJourney.ps1
```

O runner guarda cenas/assets, avalia os 17 clips no rig real, inicia a cena exterior sozinha, percorre a ida e volta de avião, exercita as três portas e verifica novamente as cenas guardadas. Termina com erro se o processo da CLI, o envelope da resposta, o script C# ou uma asserção falhar. Não confia apenas no exit code 0 da CLI.

| Cenário | Proteção |
| --- | --- |
| `Humanoid17Clips` | Avatar Humanoid, clips e cinco amostras de deformação por clip |
| `ColdStart` | Entrada carregada sozinha, seguida da inicialização real das duas cenas exteriores |
| `PlayFramesAdvance` | FJ-011: frames avançam e a sessão termina a inicialização, mesmo que o Editor estivesse sem foco antes da preparação |
| `AircraftJourney` | Caminhada exterior, embarque, voo pilotado por input virtual, aterragem, desembarque, visita ao mundo e voo de volta; blocos de entrada remota, saída em voo/sem chão e colisão com obstáculo |
| `ThreeReturnDoors` | Entrada e regresso pelas três regiões; conservação das instâncias, piso, mundo descarregado, uma câmara/motor e porta pronta para reutilização |
| `SavedSceneIntegrity` | Referências guardadas, dois locais de aterragem, contagem de arte, `.meta` geridos pela Unity, materiais/scripts, câmaras/listeners e compilação/Console |

Os testes de input isolam teclado, rato e comandos físicos quando necessário e restauram os dispositivos num bloco `finally`. O runner deixa o Editor fora de Play. Se a ligação cair durante uma execução, o runner não volta a despachar o mesmo cenário para evitar duas execuções concorrentes.

## Evidência e limites

`Docs/Regressao_Ultima_Execucao.json` resume a execução mais recente. Os relatórios específicos permanecem em `Docs/Aviao_Playtest.json`, `Exterior_Playtest.json`, `Sidekick_v02_Animations_Test.json` e `Aviao_Validacao.json`. Saídas completas temporárias ficam em `TestResults/`, fora do Git. JSON/capturas são evidência; os scripts são os testes repetíveis.

O voo da suite usa os controlos reais com dispositivos virtuais e não altera diretamente a posição do avião para atravessar o espaço. O cenário específico das três portas coloca o avião temporariamente no destino para isolar a conservação de estado, sem alegar voo. A aproximação às portas interiores distantes é colocada por código; não valida todos os percursos do mapa. O comando PS5 físico, uma build independente e os bugs FJ-001/FJ-002 continuam a precisar de validação própria.

A suite corre localmente contra o Editor aberto. Ainda não existe um job de Unity no GitHub Actions; não confundir o envio ao GitHub com execução automática no servidor. A cobertura será ampliada com cada bug e mecânica, incluindo testes pequenos no Unity Test Framework quando forem apropriados.

Integridade do backup, depois de preparar o stage: `python Tools/Verify-Repository.py`. Verifica pares de assets/pastas e `.meta`, ausência de caches/repos embutidos, ponteiros LFS e assets binários acima de 1 MiB ainda fora do LFS. O caso FJ-012 foi reproduzido e teve teste a falhar antes de acrescentar as regras para terreno, baking, SpeedTree, base de dados e biblioteca nativa. Complementar com `git lfs fsck` e comparação do commit local com a referência remota após o envio.
