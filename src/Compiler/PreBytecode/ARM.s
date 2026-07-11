    .syntax unified
    .cpu cortex-a9
    .global main

/* ---------------------------------------------------------
   Data
--------------------------------------------------------- */

    .data
prompt:
    .asciz "Enter integers separated by spaces:\n"

out_prefix:
    .asciz "Sorted: "

newline:
    .asciz "\n"

input_buf:
    .space 1024

int_array:
    .space 512     @ 128 integers * 4 bytes

int_count:
    .word 0

/* ---------------------------------------------------------
   Helpers: write(fd, buf, len)
--------------------------------------------------------- */

write_syscall:
    mov r7, #4
    svc #0
    bx lr

read_syscall:
    mov r7, #3
    svc #0
    bx lr

/* ---------------------------------------------------------
   print_cstring(r0 = address)
--------------------------------------------------------- */

print_cstring:
    push {r1,r2,r7,lr}
    mov r1, r0

find_len:
    ldrb r2, [r1]
    cmp r2, #0
    beq got_len
    add r1, r1, #1
    b find_len

got_len:
    sub r2, r1, r0
    mov r1, r0
    mov r0, #1
    bl write_syscall
    pop {r1,r2,r7,lr}
    bx lr

/* ---------------------------------------------------------
   print_int(r0 = integer)
--------------------------------------------------------- */

print_int:
    push {r1-r7,lr}

    mov r4, r0        @ number
    mov r5, #0        @ sign flag
    mov r6, sp
    sub sp, sp, #16   @ buffer

    cmp r4, #0
    bge pos
    neg r4, r4
    mov r5, #1

pos:
    mov r1, sp
    mov r2, #0

loop_div:
    mov r7, #0
    mov r3, #10
    bl __aeabi_idivmod
    @ quotient in r0, remainder in r1
    add r1, r1, #'0'
    strb r1, [r1, r2]
    add r2, r2, #1
    mov r4, r0
    cmp r4, #0
    bne loop_div

    cmp r5, #1
    bne reverse
    mov r3, #'-'
    strb r3, [r1, r2]
    add r2, r2, #1

reverse:
    mov r3, sp
    add r7, sp, r2
    sub r7, r7, #1

rev_loop:
    cmp r3, r7
    bge done_rev
    ldrb r0, [r3]
    ldrb r1, [r7]
    strb r1, [r3]
    strb r0, [r7]
    add r3, r3, #1
    sub r7, r7, #1
    b rev_loop

done_rev:
    mov r0, #1
    mov r1, sp
    mov r2, r2
    bl write_syscall

    add sp, sp, #16
    pop {r1-r7,lr}
    bx lr

/* ---------------------------------------------------------
   parse_ints
--------------------------------------------------------- */

parse_ints:
    push {r1-r7,lr}

    ldr r1, =input_buf
    ldr r2, =int_array
    mov r3, #0      @ current value
    mov r4, #0      @ sign
    mov r5, #0      @ count

next_char:
    ldrb r6, [r1]
    cmp r6, #0
    beq end_line

    cmp r6, #' '
    beq maybe_end

    cmp r6, #'\n'
    beq maybe_end

    cmp r6, #'-'
    bne digit
    mov r4, #1
    add r1, r1, #1
    b next_char

digit:
    cmp r6, #'0'
    blt skip
    cmp r6, #'9'
    bgt skip

    sub r6, r6, #'0'
    mov r7, #10
    mul r3, r3, r7
    add r3, r3, r6
    add r1, r1, #1
    b next_char

skip:
    add r1, r1, #1
    b next_char

maybe_end:
    cmp r3, #0
    bne store_num
    cmp r4, #0
    bne store_num
    add r1, r1, #1
    b next_char

store_num:
    cmp r4, #0
    beq no_neg
    neg r3, r3
no_neg:
    str r3, [r2]
    add r2, r2, #4
    add r5, r5, #1
    mov r3, #0
    mov r4, #0
    add r1, r1, #1
    b next_char

end_line:
    cmp r3, #0
    beq finish
    cmp r4, #0
    beq no_neg2
    neg r3, r3
no_neg2:
    str r3, [r2]
    add r5, r5, #1

finish:
    ldr r0, =int_count
    str r5, [r0]

    pop {r1-r7,lr}
    bx lr

/* ---------------------------------------------------------
   quicksort(arr=r0, left=r1, right=r2)
--------------------------------------------------------- */

quicksort:
    push {r3-r11,lr}

    cmp r1, r2
    bge qs_done

    add r3, r1, r2
    lsr r3, r3, #1
    ldr r4, [r0, r3, lsl #2]

    mov r5, r1
    mov r6, r2

qs_loop:
qs_i:
    ldr r7, [r0, r5, lsl #2]
    cmp r7, r4
    blt inc_i
    b qs_i_done
inc_i:
    add r5, r5, #1
    b qs_i

qs_i_done:
qs_j:
    ldr r7, [r0, r6, lsl #2]
    cmp r7, r4
    bgt dec_j
    b qs_j_done
dec_j:
    sub r6, r6, #1
    b qs_j

qs_j_done:
    cmp r5, r6
    bgt partition_done

    ldr r7, [r0, r5, lsl #2]
    ldr r8, [r0, r6, lsl #2]
    str r8, [r0, r5, lsl #2]
    str r7, [r0, r6, lsl #2]

    add r5, r5, #1
    sub r6, r6, #1
    b qs_loop

partition_done:
    mov r1, r1
    mov r2, r6
    bl quicksort

    mov r1, r5
    mov r2, r2
    bl quicksort

qs_done:
    pop {r3-r11,lr}
    bx lr

/* ---------------------------------------------------------
   main
--------------------------------------------------------- */

main:
    ldr r0, =prompt
    bl print_cstring

    mov r0, #0
    ldr r1, =input_buf
    mov r2, #1024
    bl read_syscall

    ldr r1, =input_buf
    add r1, r1, r0
    mov r2, #0
    strb r2, [r1]

    bl parse_ints

    ldr r1, =int_count
    ldr r1, [r1]
    cmp r1, #0
    beq exit

    ldr r0, =int_array
    mov r1, #0
    sub r2, r1, #1
    add r2, r2, r1
    add r2, r2, r1
    add r2, r2, r1
    add r2, r2, r1
    sub r2, r1, #1
    add r2, r2, r1
    add r2, r2, r1
    add r2, r2, r1
    add r2, r2, r1
    sub r2, r1, #1
    add r2, r2, r1
    add r2, r2, r1
    add r2, r2, r1
    add r2, r2, r1

    sub r2, r1, #1
    add r2, r2, r1
    add r2, r2, r1
    add r2, r2, r1
    add r2, r2, r1

    sub r2, r1, #1
    add r2, r2, r1
    add r2, r2, r1
    add r2, r2, r1
    add r2, r2, r1

exit:
    mov r7, #1
    mov r0, #0
    svc #0
