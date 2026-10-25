# UmaTank - documentação do projeto

Desenvolvemos o UmaTank a partir do jogo de tanques da atividade de Agentes Inteligentes. Conversamos em sala com o professor Eduardo sobre reaproveitar o projeto e seus modelos, e ele autorizou. Mantivemos a movimentação com A* e acrescentamos a escolha de personagens, as habilidades, o limite de tempo e o registro das partidas.

O objetivo é eliminar dez inimigas em até três minutos. Se o jogador alcançar a meta, vence; se perder toda a vida, perde; se o tempo acabar antes da meta, empata. O ranking considera o menor tempo de vitória de cada jogador.

## Onde encontrar as explicações

| Assunto | Arquivo |
| --- | --- |
| Regras, controles, organização, dados salvos, configuração, testes, autoria e uso de IA | [Comentário inicial de BattleSessionController.cs](../Assets/Scripts/Game/BattleSessionController.cs) |
| Comunicação com o PHP e salvamento das partidas | [DataManager.cs](../Assets/Scripts/Backend/DataManager.cs) |
| A* reaproveitado e adaptações | [AStarPathfinder.cs](../Assets/Scripts/Pathfinding/AStarPathfinder.cs) |
| Comportamento das inimigas, movimento e combate | Comentários em `Assets/Scripts/Pathfinding` e `Assets/Scripts/Tanks` |
| Habilidades das aparências | [Skins_UniqueSkills.md](Skins_UniqueSkills.md) |
| Como colocar as imagens das habilidades e os áudios | [Guia_JP_Imagens_e_Vozes.pdf](Guia_JP_Imagens_e_Vozes.pdf) |

## Como abrir e configurar

1. Abra o projeto na Unity 6000.3.5f2 e a cena `Assets/Scenes/UmaTank.unity`.
2. Copie `Assets/Backend/gsd` para a pasta `htdocs/gsd` do XAMPP e ligue o Apache e o MySQL.
3. No phpMyAdmin, crie um banco vazio chamado `tank_gamers` e importe `Assets/Backend/tank_gamers.sql`. Esse arquivo inclui as tabelas e os dados de teste.
4. Ajuste a conexão em `gsd/model/DB.php` com o usuário e a senha do MySQL. O SQL não cria esse usuário.
5. Confira o endereço em `DataManager.URL_BASE`. Para jogar no mesmo computador do XAMPP, usamos `http://localhost/gsd`.
6. Cadastre uma conta, faça login, escolha uma personagem e inicie a partida. O histórico e a análise ficam em `/gsd/?action=partidas`, após o login da página web.

A entrega inclui as pastas `Assets`, `Packages` e `ProjectSettings` do projeto, o PHP e o arquivo SQL.

## Testes

Conferimos a seleção, as habilidades, os tiros, a movimentação, a pausa e o retorno ao menu. Também testamos login válido e inválido, o salvamento de vitória, derrota e empate, o histórico e o ranking por tempo. Repetir o envio de uma mesma partida não deve criar outro registro.

Restauramos o SQL em um banco separado e verificamos os dados salvos pela Unity através do PHP. Esses testes não alteraram o banco usado pelo grupo. A conferência foi feita no projeto da Unity; não geramos uma nova build executável nessa revisão.

## Créditos

As personagens, os modelos originais, as falas e as imagens são de **Umamusume: Pretty Derby**, da **Cygames**. Todos os direitos desses materiais pertencem à Cygames. Usamos modelos extraídos do jogo e disponibilizados por fãs no Sketchfab.

A música da tela de derrota é **[Sad game over](https://opengameart.org/content/sad-game-over)**, de **Emma_MA**, disponibilizada em **CC0**.

Os links das músicas, dos sons, dos modelos e dos demais assets estão no comentário principal e nos arquivos `Credits.txt` das pastas correspondentes. Algumas fontes de outros materiais ainda precisam ser completadas; as licenças já identificadas estão registradas nesses arquivos.
