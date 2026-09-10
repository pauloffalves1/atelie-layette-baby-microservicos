# language: pt
Funcionalidade: Cancelamento de pedido pelo cliente
    Como cliente do ateliê
    Eu quero poder cancelar meu próprio pedido
    Para desistir de uma compra antes que a produção comece

Contexto:
    Dado que existe um pedido feito por "Maria Silva" com status "Recebido"

Cenário: Cliente cancela um pedido que ainda está como Recebido
    Quando a própria cliente pede o cancelamento do pedido
    Então o pedido passa a ter o status "Cancelado"

Esquema do Cenário: Cliente não pode cancelar pedido que já saiu de Recebido
    Dado que o pedido muda para o status "<status>"
    Quando a própria cliente pede o cancelamento do pedido
    Então o cancelamento é recusado com a mensagem "Só é possível cancelar o pedido enquanto ele estiver como 'Recebido' — fale conosco pelo WhatsApp se a produção já começou."

    Exemplos:
        | status      |
        | EmProducao  |
        | Pronto      |
        | Enviado     |
        | Entregue    |

Cenário: Cliente não pode cancelar um pedido de outro cliente
    Quando outra cliente pede o cancelamento do pedido
    Então o cancelamento é recusado por não encontrar o pedido
