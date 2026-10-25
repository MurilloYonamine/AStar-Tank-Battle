# UmaTank - documentação do projeto

Esta atividade é da disciplina de **Games em Sistemas Distribuídos**. Reaproveitamos o jogo de tanques que desenvolvemos em **Inteligência Artificial para Jogos**, com autorização do professor Eduardo, mantendo seus modelos e a movimentação com A*. O código original do pathfinding veio de uma atividade de **Agentes Inteligentes** do semestre anterior, cujo reaproveitamento também foi autorizado.

Ao longo do desenvolvimento, acrescentamos a escolha de personagens, as habilidades, o limite de tempo e o registro das partidas.

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

Outras fontes usadas no projeto:

| Asset | Fonte | Onde foi usado |
| --- | --- | --- |
| Low poly tree, de kiprus | [Free3D](https://free3d.com/3d-model/low_poly_tree-816203.html) | Árvores do cenário. |
| Stylized bush | [Sketchfab](https://sketchfab.com/3d-models/stylized-bush-9d9ce79d3ae040619e96d5b22c7de1a6) | Arbustos do cenário. |
| Animações de dano, ataque, dança/vitória e morte | [Adobe Mixamo](https://www.mixamo.com/) | Animações das personagens. |
| Ultimate UI SFX Pack, de JDSherbert | [itch.io](https://jdsherbert.itch.io/ultimate-ui-sfx-pack) | Sons do menu. |
| 2 High Quality Explosions, de Michel Baradari | [OpenGameArt](https://opengameart.org/content/2-high-quality-explosions) | Sons dos tiros. |
| Roboto Regular, de Christian Robertson / The Roboto Project Authors | [Repositório da fonte](https://github.com/googlefonts/roboto-3-classic) | Textos do menu. Licença SIL OFL 1.1 incluída em `Assets/Fonts/Roboto-OFL.txt`. |

Os demais links estão no comentário principal e nos arquivos `Credits.txt` das pastas correspondentes. Ainda faltam os links individuais de alguns modelos adicionados depois e a identificação da música original do trecho chamado de “WAN WAN”.
