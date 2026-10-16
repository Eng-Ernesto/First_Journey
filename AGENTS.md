# First Journey — regras de trabalho

## Regra obrigatória de testes (decisão do criador, 16 outubro 2026)

Antes de corrigir um bug, registar o sintoma, os passos para o reproduzir, o resultado esperado e o resultado observado em `Docs/Bugs.md`. Criar um teste automatizado que reproduza esse bug e confirmar que falha pelo motivo esperado. Só depois implementar a correção mínima, confirmar que o teste passa e executar a regressão relevante.

Manter o teste de cada bug corrigido no projeto. Não apagar, enfraquecer ou marcar um teste como aprovado para contornar uma falha. Ao alterar gameplay, controlos, personagem, animações, cenas ou transições, executar os testes dos comportamentos afetados e a suite de regressão antes de concluir a integração. Novos comportamentos precisam de cobertura automatizada adequada. Um bug sem reprodução/teste confirmado continua pendente.

Os relatórios JSON e as capturas são evidência de uma execução; não substituem os testes executáveis. Não apresentar dispositivos virtuais como testes do comando físico, nem colocação por código como voo ou caminhada reais. Não declarar uma build ou um envio concluído sem confirmação.

Comando da suite local, com o Editor aberto e Pipeline ativo:

```powershell
pwsh -File Tools/Test-FirstJourney.ps1
```

O comando usa o plugin oficial Unity CLI/Pipeline, falha se qualquer verificação falhar, restaura os dispositivos de input e deixa o Editor fora de Play. Ver `Docs/Testes_Automatizados.md` para cobertura e limitações. Alterações exclusivamente documentais exigem revisão do conteúdo e dos links, sem testes artificiais de gameplay.

## Continuidade e preservação

- Ler o handoff oficial do Notion e a documentação atual antes de assumir decisões. Registos históricos não representam necessariamente o estado atual.
- Operar cenas, prefabs e assets pelo plugin oficial Unity CLI/Pipeline; não editar YAML da Unity à mão.
- Preservar a arte original e o visual provisório autorizado; manter alterações incrementais. Modelação usa o fluxo escolhido pelo criador, Mixar.
- Entrada atual: `Assets/_FirstJourney/Scenes/FirstJourney_EspacoExterior.unity`. Base e asteroide partilham o exterior; Fantasy Kingdom contém três regiões no mesmo mundo.
- Antes de confirmar backup: guardar cenas/assets, verificar o conteúdo staged, pares `.meta`, exclusão de caches e Git LFS; verificar `git lfs fsck` e igualdade entre HEAD e a referência remota após o push.
