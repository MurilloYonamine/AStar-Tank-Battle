# Unique Skills das aparências

Cada aparência tem seus próprios atributos e sua Unique Skill. São 25 opções e 22 personagens diferentes. Ao escolher uma personagem, suas outras aparências também ficam fora da seleção de inimigas. Por exemplo, escolher a Doto normal ou Halloween impede que qualquer uma das duas apareça como inimiga.

Adaptamos as habilidades para combinar com o combate do UmaTank. Os efeitos não são iguais aos do jogo original. “Bazuca” e “Xen” são nomes que usamos para identificar essas aparências no projeto.

| Aparência | Unique Skill | Efeito no UmaTank |
| --- | --- | --- |
| Meisho Doto — Halloween | Travessura à meia-noite | Por 8 s: +35% de velocidade, −20% no intervalo de tiro e −10% de dano recebido. |
| Super Creek — Múmia | Abraço da múmia | Recupera 25% da vida máxima ao longo de 6 s; recebe 15% menos dano durante o efeito. |
| Super Creek — Shinobi | Caminho da sombra | Por 7 s: +60% de velocidade e −15% no intervalo de tiro. Não fica invisível nem atravessa obstáculos. |
| Mayano Top Gun — Halloween | Voo da bruxinha | Por 7 s: +40% de velocidade e −20% no intervalo de tiro. Não voa sobre obstáculos nem recupera vida. |
| Oguri Cap — Natal | Milagre da noite santa | Recupera 25% da vida máxima ao ativar; por 8 s, +20% de velocidade e −10% no intervalo de tiro. |
| Gold Ship — Bazuca | Surpresa de bazuca | Próximo tiro em até 10 s: +70% de dano e +100% de raio da explosão. |
| Yaeno Muteki — Xen | Impacto Xen | Próximo tiro em até 8 s: +60% de dano e +50% de raio da explosão. |

A habilidade carrega com o tempo e ao eliminar inimigas: são 2 pontos por segundo e 15 por eliminação, até completar 100. Quando estiver pronta, SHIFT ativa a Unique Skill. Ela não recarrega enquanto está ativa. Ao pausar, a carga, a duração e a cura também ficam paradas.

Nas habilidades que fortalecem um disparo, o bônus vale para o próximo tiro, mesmo que ele erre ou bata em um obstáculo. O tanque não recebe dano do próprio tiro. As inimigas usam os atributos e as vantagens passivas de suas aparências, mas não ativam Unique Skills.

## Onde ajustar

As configurações ficam em `Assets/Settings/CharacterClasses`. Na cena `Assets/Scenes/UmaTank.unity`, cada personagem da lista **Characters**, no `CharacterSelectionController`, usa a configuração escolhida no campo **Class Definition**.

Usamos como referência as personagens de Umamusume: Pretty Derby, incluindo os perfis de [Meisho Doto](https://umamusume.jp/character/meishodoto/), [Super Creek](https://umamusume.jp/character/supercreek) e [Mayano Top Gun](https://umamusume.jp/character/mayanotopgun). Os efeitos de tanque descritos aqui foram criados para o nosso jogo.

## Testes

Conferimos a ativação por SHIFT, a carga, a duração e os efeitos das sete aparências da tabela. Também verificamos os limites de cura, os tiros fortalecidos, o funcionamento da pausa e o fim dos bônus. A escolha de uma personagem exclui suas outras aparências das inimigas.

Os testes incluíram o restante da partida: seleção, contagem, movimento, tiro, vitória, derrota, empate, salvamento e volta ao menu. A avaliação do equilíbrio e da sensação de cada habilidade também depende de jogar com elas.
