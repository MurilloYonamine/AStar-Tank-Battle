# UmaTank - integração e documentação da entrega

## Onde está a documentação obrigatória

A documentação exigida na Atividade 2 está dentro dos scripts da Unity, não depende de um relatório separado. Este arquivo é apenas um guia de localização.

| Assunto | Arquivo |
| --- | --- |
| Nome do jogo e integrantes, reaproveitamento autorizado, objetivo, controles, regras, tempo/ranking, coleta e retenção de dados, configuração, SQL, observação dos testes, autoria/IA e créditos | [Comentário inicial de BattleSessionController.cs](../Assets/Scripts/Game/BattleSessionController.cs) |
| Rotas PHP, campos enviados, JSON esperado, token e tratamento de falhas | [DataManager.cs](../Assets/Scripts/Backend/DataManager.cs) |
| Referência ao A* reaproveitado, arquivo/linhas de origem e adaptações | [AStarPathfinder.cs](../Assets/Scripts/Pathfinding/AStarPathfinder.cs) |
| Organização e funcionamento da IA, combate e movimento | Comentários das classes em `Assets/Scripts/Pathfinding` e `Assets/Scripts/Tanks` |
| Habilidades das aparências e parâmetros | [Skins_UniqueSkills.md](Skins_UniqueSkills.md) e perfis em `Assets/Settings/CharacterClasses` |
| Prints para configurar música de derrota, imagem e voz pelo Inspector | [Guia_JP_Imagens_e_Vozes.pdf](Guia_JP_Imagens_e_Vozes.pdf) |
| Créditos das falas e imagens do jogo original | `Assets/Audio/Voices/Credits.txt`, `Assets/Skills/Credits.txt` e comentário principal |

## O que foi integrado do JP em 25/10/2026

- Commits `a85d5f2` e `dde65bd`: 18 arquivos adicionais de voz, 59 ícones PNG e a música `sad_game_over.wav`.
- As vozes já escolhidas pelo JP foram mantidas nas 25 entradas da seleção; os arquivos alternativos também foram preservados.
- A música de derrota foi ligada ao campo existente, mantendo reprodução única, volumes e mixer do Murillo.
- Os ícones escolhidos pelo JP foram mantidos nos 22 perfis anteriores. As três variantes novas também receberam arte: Doto Halloween, Creek Shinobi e Yaeno Xen.
- Os perfabs `AgemasenTank` e `BonoTank` passaram a `SpecialWeekTank` e `AkebonoTank`, como renomeados pelo JP, sem mudar seus GUIDs ou perder referências.
- Os conflitos foram resolvidos mantendo as habilidades e os números das skins. Continuam 25 opções, 25 perfis e 22 famílias; outra skin da personagem escolhida não vira inimiga.
- Mapa, árvores, obstáculos, patrulha, spawn, lógica de combate, sistema PHP e SQL do grupo foram preservados. A integração ocorre apenas em `feat/murilotanquefeliz2`; a `modelsPlus` não é alterada.

## Arquivos que compõem a entrega

1. Projeto Unity: `Assets`, `Packages` e `ProjectSettings`, usando a cena `Assets/Scenes/UmaTank.unity`.
2. Middleware completo: pasta `Assets/Backend/gsd`. Na máquina de demonstração, copiar para o `htdocs/gsd` do XAMPP e configurar `model/DB.php`.
3. Banco: `Assets/Backend/tank_gamers.sql`, com estrutura e dados de teste. Importar o `.sql`, nunca o `.meta`, em um banco vazio.

A URL da Unity é configurada em `DataManager.URL_BASE`. As instruções completas de configuração e as tabelas estão no comentário inicial do script principal. O dump não cria automaticamente o usuário MySQL nem suas permissões.

## Créditos e cuidados restantes

Conforme informado pelo grupo, as falas, imagens das personagens e ícones de habilidades vieram de **Umamusume: Pretty Derby**, da **Cygames**. Todos os direitos dos materiais originais são atribuídos à Cygames; o grupo não reivindica autoria deles. Essa origem está registrada nos comentários e nos arquivos de créditos.

Isso não substitui as condições de uso dos titulares nem a identificação dos extratores/disponibilizadores dos modelos 3D. A música `sad_game_over.wav` é um material separado: sua fonte, compositor e licença ainda precisam ser informados. Também permanecem as incertezas de uso já registradas para músicas recortadas de vídeos e outros áudios, além das fontes de modelos/animações/cenário/fonte onde ainda não fornecidas.

Conforme esclarecido pelo grupo, os modelos 3D foram **extraídos do jogo original Umamusume: Pretty Derby**, da **Cygames**, e disponibilizados por fãs no **Sketchfab para uso de fãs**. Portanto, não são apresentados como modelagens originais feitas pelos disponibilizadores: os direitos dos modelos originais são atribuídos à Cygames. Os cinco links já fornecidos estão no comentário principal; as páginas e os extratores/disponibilizadores dos modelos posteriores ainda precisam ser informados. A disponibilidade para fãs é registrada como informação do grupo, sem afirmar que uma licença específica ou uma autorização irrestrita de uso tenha sido verificada.

## Validação técnica da integração

- Os scripts de produção compilam, inclusive com validação dos comentários XML.
- Passaram 3.031 verificações estáticas de integração: mapa/componentes preservados, valores das 25 habilidades mantidos, vozes escolhidas pelo JP, ícones válidos e GUIDs dos prefabs renomeados.
- Passaram 103 verificações de referências, cobrindo 692 dependências da cena.
- A regressão de lógica/combate/classes/resultados passou em 559 verificações.
- A cena integrada passou em 4.835 verificações, incluindo verificações por objeto/osso — não são 4.835 partidas. Todas as 25 opções tiveram voz decodificada, arte válida e placeholder oculto na HUD. As sete aparências especiais mantiveram habilidades próprias; a música de derrota tocou uma vez e foi substituída pelo loop do menu ao retornar.
- O SQL foi restaurado de fato em um MySQL temporário vazio e separado, recuperando os três jogadores, 15 partidas e três tokens do arquivo entregue. Dez verificações de restauração e 23 de API real passaram, cobrindo login válido/inválido, dois jogadores adicionais de teste, novas partidas, histórico, ranking, empates, validação de campos, token de outro jogador e análise web autenticada.
- A Unity autenticou pela cópia real do middleware e gravou vitória, derrota e empate nesse MySQL temporário. Cinco verificações adicionais confirmaram diretamente os registros persistidos. O arquivo SQL entregue não recebeu essas partidas artificiais; elas existem apenas na instância de validação.

Os testes não modificaram o banco original do grupo. A conexão configurada no XAMPP principal não estava acessível na conferência desta sessão; conferir Apache/MySQL e as credenciais locais antes da demonstração. As alterações de configuração e arquivos gerados pela Unity que já estavam locais não fazem parte da integração.

Não houve exceções de gameplay nos cenários da cena integrada. O Editor ainda pode registrar o erro interno de indexação de `UnityEditor.Search` já observado anteriormente. Não foi gerada nem testada uma nova build executável nesta etapa.

Uma nova build executável não é exigida pelo PDF da Atividade 2. Não usar as builds antigas como se contivessem esta integração; gerar e testar outra se o grupo decidir entregar um executável. O nome de um commit não altera o cumprimento dos requisitos da atividade.
