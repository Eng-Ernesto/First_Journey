# Ilha do avião — versão 01

A cena `Assets/_FirstJourney/Scenes/FirstJourney_LuaBase.unity` contém agora a ilha da casa e uma segunda ilha destinada ao avião.

## Composição

- Nova ilha com aproximadamente 17,8 × 14,2 metros, na posição Unity `(24, 0, 4.5)`.
- Terreno, materiais, árvores, pedras e plantas reutilizados da arte original da lua-base.
- Clareira de terra com cerca de 11,8 × 9,3 metros para o estacionamento.
- Duas árvores nas bordas, oito pedras, quatro grupos de plantas e quatro manchas de musgo.
- Entrada de cerca de 1,9 metros de largura voltada para a ilha da casa. A ponte foi acrescentada na etapa seguinte, descrita em `PonteEntreIlhas.md`.
- Marcador `Encaixe_Futura_Ponte` na nova ilha, ao nível do terreno. Marcador `Ponto_Interacao_Aviao` para orientar a futura interação.

## Avião original

Origem: `C:\Ernesto\unity\assets\autronauta.mixar`, cena `Cria O Avi O Do Pequeno`.

Foram exportadas apenas as 38 peças `aviao_aviador_*`. O astronauta, o cubo auxiliar, as câmaras e as luzes do ficheiro ficaram excluídos. O avião mantém aproximadamente 8 metros de envergadura e 6,1 metros de comprimento. Está estacionado, com os pneus ao nível da clareira.

O ficheiro Mixar original não foi guardado nem alterado pelo processo de conversão. O avião foi convertido numa execução separada do Mixar e importado/remapeado através do plugin oficial Unity CLI/Pipeline. Os nove materiais originais foram convertidos para URP Lit; a transparência do para-brisas aproxima a transmissão do material original.

## Ficheiros editáveis e revisão

- `Assets/_FirstJourney/Prefabs/AirfieldIsland.prefab`: terreno, decoração e avião colocado.
- `Assets/_FirstJourney/Prefabs/Airplane.prefab`: avião separado, sem astronauta.
- `Assets/_FirstJourney/Art/AirfieldIsland/Meshes`: superfícies da clareira, caminho e musgo.
- `Assets/_FirstJourney/Art/Airplane`: modelo FBX, materiais e manifesto da exportação.
- `Assets/_FirstJourney/Scenes/Versions/FirstJourney_LuaBase_AntesIlhaAviao.unity`: cópia da cena anterior à nova ilha.
- `Assets/_FirstJourney/Review/DuasIlhas_Unity_v01.png`: vista geral.
- `Assets/_FirstJourney/Review/IlhaAviao_Unity_v01.png`: detalhe da nova ilha.
- `AgentScripts/AirfieldIsland.cs`: construção e verificações via Pipeline.
- `AgentScripts/export_airplane.py`: conversão do avião, sem guardar o original.
- `Docs/AirfieldIsland_Result.json`: resultado das verificações na cena.

## Verificação

As 584 partes visíveis da lua-base mantêm-se. A nova ilha tem 104 renderers e oito colisores estáticos, incluindo terreno, troncos, fuselagem, asas e pneus. As extremidades do avião e a entrada para a futura ponte foram verificadas por raios contra o terreno. Não há materiais ausentes nem partes do astronauta no avião. A cena foi verificada em Play Mode, sem erros atuais de compilação ou execução.

A ponte e o caminho de acesso já estão presentes. A travessia foi verificada com um controlador temporário e depois com o AJ do Mixamo, agora jogável com teclado/rato e DualSense. Ver `HeroAJ_Integracao.md`. A entrada no avião e o voo continuam pendentes.
