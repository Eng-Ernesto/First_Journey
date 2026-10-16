# Avaliação do Synty Sidekick — 15 outubro 2026

**Atualização de 16 outubro:** a primeira versão jogável do protagonista Sidekick já foi instalada no lugar do AJ. O estado atual, exportação FBX e testes estão em `Sidekick_Integracao_v01.md`; esta avaliação regista a investigação inicial.

## Resultado

**Decisão posterior do criador, em 15 outubro:** o Sidekick é a ferramenta adotada para os personagens e será usado para substituir o AJ. A sessão foi encerrada após a validação de acesso; a migração fica para a próxima sessão. Ver `Personagens_Sidekick.md`.

O Sidekick Character Creator 1.2.7 está instalado e acessível pelo plugin oficial Unity CLI/Pipeline no First Journey. A janela aberta é `Synty.SidekickCharacters.ModularCharacterWindow`; a API pública de criação é `Synty.SidekickCharacters.API.SidekickRuntime`.

Consigo automatizar a montagem de personagens, seleção de peças, proporções corporais, cores, expressões e ligação a um Animator através de scripts executados pelo plugin. Não dependo de cliques manuais na janela para estas operações. A exportação para FBX e a gravação de personagens existem no Editor, mas não foram executadas nesta investigação.

## Conteúdo efetivamente instalado

- 157 peças descobertas pela ferramenta, distribuídas por 38 categorias.
- Conteúdo humano e filtros de presets `Sci-fi - Civilians` e `Fantasy - Knights`.
- Duas opções de cabeça, onze de cabelo, onze de nariz; três opções por categoria principal de torso e membros, além de olhos, sobrancelhas, orelhas, barba, dentes e acessórios.
- Demos de personalização de peças, presets e cores em runtime, e de animação facial.
- API em C#, base de dados local, materiais e shader próprios.
- Shader `Shader Graphs/Sidekick_ShaderGraph` reconhecido como suportado pelo Editor atual. Isto não substitui uma revisão visual final de um personagem no cenário.
- FBX Exporter 5.1.6 e Shader Graph 17.3.0 presentes no manifest; URP continua em 17.3.0.

`Unrestricted` também aparece como categoria de espécie na base de dados; não representa uma segunda espécie de personagem confirmada. Não foi encontrado um conjunto de peças de cachorro nesta instalação.

## Possibilidades para First Journey

| Utilização | Aplicação ao projeto | Situação |
| --- | --- | --- |
| Personagens humanos modulares | Criar candidatos de piloto ou NPCs e variar roupa, cabelo, pele e acessórios | Viável com as peças instaladas; o visual precisa de aprovação |
| Proporções corporais | Ajustar silhueta, constituição e musculatura | Alteração pela API testada |
| Expressões faciais | Preocupação com o cachorro, felicidade no regresso, piscar e fala | Blendshapes disponíveis; expressão de boca testada |
| Animações Mixamo existentes | Aproveitar o trabalho de locomoção já feito para o AJ | Corrida do Animator atual testada no rig Sidekick |
| Roupa e equipamento intercambiáveis | Variações de roupa e acessórios, eventualmente num menu | API e demos disponíveis; mecânica ainda por construir |
| Personagem combinado | Produzir um modelo com menos renderers a partir das peças | Combinação de 24 peças num renderer testada |
| Prefab e FBX | Guardar uma versão editável e preparar o personagem para o jogo | Funções presentes; exportação ainda não testada |

## Teste prático pelo plugin Unity

O script `AgentScripts/SidekickAccessProbe.cs` criou um personagem técnico numa cena de preview temporária, usando peças instaladas, material próprio temporário e uma instância separada da API.

- 24 peças combinadas num SkinnedMeshRenderer: 7 641 vértices e 88 blendshapes nesta combinação específica.
- Avatar Humanoid válido.
- Peso de musculatura alterado pela API de 40 para 80.
- Expressão `jawOpen` alterada e confirmada.
- Atualização de cor pela API testada numa textura temporária; não alterou as texturas do projeto.
- Animator `AJ_Hero.controller` aplicado ao modelo Sidekick: movimento real da perna observado na corrida, com variação de cerca de 25,86 graus.
- Modelo, malhas de teste, material, textura e cena de preview eliminados após a verificação.
- Objetos raiz da cena principal preservados; a cena estava sem alterações pendentes antes e depois do teste.

Relatório completo: `Docs/Sidekick_Access_Probe.json`. A compatibilidade foi comprovada para a corrida; os restantes clips e o comportamento completo no cenário ainda precisam de validação individual.

## Limites e abordagem recomendada

O Sidekick é um sistema de peças preparadas para o seu esqueleto, materiais e blendshapes. Não transforma automaticamente o AJ ou o avatar de referência num personagem modular, nem reproduz qualquer roupa ou rosto a partir de uma imagem. Peças próprias exigem modelação e adaptação ao sistema.

O próximo trabalho é criar o protagonista no Sidekick, com paleta alinhada ao cenário, e substituir o AJ reaproveitando as animações existentes. A decisão de substituir já foi tomada pelo criador; o AJ permanece instalado provisoriamente até essa migração.

A criação de personagem durante o jogo é possível pela API, mas o guia alerta que a combinação de malhas é pesada. Para o protótipo, preparar o personagem no Editor e usar o prefab pronto; personalização em runtime pode ser uma etapa posterior.

Esta investigação não alterou os ficheiros da Synty, o cenário ou o AJ, nem instalou pacotes adicionais. Não foram criadas novas mecânicas de jogo ou exportações de personagem.

## Fontes consultadas

- Guia local, 33 páginas: `Assets/Synty/SidekickCharacters/Documentation/SidekickCharacters_UserGuide.pdf`, especialmente propriedades, runtime e Mecanim.
- Código instalado: `SidekickRuntime.cs`, `ModularCharacterWindow.cs`, `SidekickFacialController.cs` e demos de runtime.
- [Descrição oficial do Starter Pack e funcionalidades](https://syntystore.com/en-gb/products/sidekick-modular-characters-starter-pack).
- [Changelog oficial](https://syntystore.com/pages/sidekicks-changelog): a versão 1.2.7 inclui correções na avaliação do Avatar exportado e no ajuste de roupa ao corpo.

