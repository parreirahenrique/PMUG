Esse algoritmo foi criado como parte principal do meu Trabalho Final de Curso para formação em Engenharia Elétrica. Nesse trabalho, tem-se como objetivo solucionar o problema do planejamento da manutenção de unidades geradoras (ou PMUG) de um SEP inserido no mercado de energia descentralizado. Para fazer isso, uma metodologia baseada em um algoritmo multiobjetivo de otimização é proposta.

O algoritmo metaheurístico NSGA-II é então adaptado e empregado a fim de criar cronogramas de manutenção dessas UGs de modo que sejam identificados os melhores momentos dentro do ano para que isso possa ocorrer.

Alguns pontos devem ser observados, no entanto, sobre o período em que as unidades entram em manutenção, sendo eles:

•	Devem ser selecionados momentos estratégicos de modo que a carga demandada possa ser suprida pelas demais unidades de geração do SEP.

•	No processo de geração de energia há um custo de produção, que é dependente do combustível utilizado para realizar essa geração. Logo, é interessante que essa manutenção ocorra de modo que o preço pela energia não seja absurdo, de modo a impactar os consumidores de energia elétrica.

•	Também, dadas as características de descentralização do mercado ligadas ao setor elétrico, é necessário que se leve em consideração os interesses particulares de empresas privadas que atuam nesse segmento. Sendo assim, a receita média dos produtores deve ser lavada em consideração, de modo que eles não sejam prejudicados financeiramente quando essas manutenções ocorrem.

Nesse algoritmo, o problema PMUG é então solucionado levando em consideração três interesses gerais: o índice de confiabilidade do SEP, o custo médio de produção de energia e a receita média do produtor.

O algoritmo é responsável por criar planos de manutenção para oito UGs de uma determinada companhia fictícia, sendo utilizado o sistema teste IEEE-RTS para representar o SEP sendo estudado.

Os parâmetros de entrada, necessários ao funcionamento do algoritmo, se encontram no diretório PMUG/ProgMntUG/bin/Debug/DADOS DE ENTRADA. Já os resultados obtidos, i.e., os cronogramas gerados pelo algoritmo, se encontram no diretório PMUG/ProgMntUG/bin/Debug, onde podem ser encontradas pastas com o nome "RESULT-" seguidos da data em que o algoritmo foi executado.
