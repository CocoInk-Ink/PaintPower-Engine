; build: nasm -f elf32 sort_ints.asm && gcc -m32 sort_ints.o -o sort_ints
; run:   ./sort_ints
; input example:  10 3 5 7 2 9

SECTION .data
    prompt_msg     db "Enter integers separated by spaces:", 10, 0
    prompt_len     equ $ - prompt_msg

    out_prefix     db "Sorted: ", 0

    buf_size       equ 1024
    input_buf      times buf_size db 0

    int_array      times 128 dd 0      ; up to 128 integers
    int_count      dd 0

    nl             db 10, 0

SECTION .bss
    ; no extra bss needed

SECTION .text
    global main

; ------------------------------------------------------------
; write(fd, buf, len)
;   eax = 4, ebx = fd, ecx = buf, edx = len
; ------------------------------------------------------------
write:
    mov     eax, 4
    int     0x80
    ret

; ------------------------------------------------------------
; read(fd, buf, len)
;   eax = 3, ebx = fd, ecx = buf, edx = len
;   returns bytes read in eax
; ------------------------------------------------------------
read:
    mov     eax, 3
    int     0x80
    ret

; ------------------------------------------------------------
; print_cstring: prints zero-terminated string to stdout
;   arg: ecx = address of string
; ------------------------------------------------------------
print_cstring:
    push    ecx
    mov     edi, ecx
    xor     eax, eax
.find_len:
    cmp     byte [edi], 0
    je      .got_len
    inc     edi
    jmp     .find_len
.got_len:
    mov     edx, edi
    sub     edx, ecx        ; length
    mov     ebx, 1          ; stdout
    mov     eax, 4
    int     0x80
    pop     ecx
    ret

; ------------------------------------------------------------
; print_int: prints signed 32-bit integer in eax
;   clobbers: eax, ebx, ecx, edx
; ------------------------------------------------------------
print_int:
    ; buffer for digits (max 11 chars + sign)
    push    ebp
    mov     ebp, esp
    sub     esp, 16         ; local buffer on stack

    mov     ebx, esp        ; buf start
    mov     ecx, 0          ; digit count

    ; handle zero specially
    cmp     eax, 0
    jne     .not_zero
    mov     byte [ebx], '0'
    mov     ecx, 1
    jmp     .build_done

.not_zero:
    ; handle sign
    mov     edx, 0
    cmp     eax, 0
    jge     .positive
    neg     eax
    mov     edx, 1          ; sign flag

.positive:
    ; convert to decimal (reverse order)
.convert_loop:
    mov     ebp, 10
    xor     edx, edx
    div     ebp             ; eax = eax/10, edx = remainder
    add     dl, '0'
    mov     [ebx + ecx], dl
    inc     ecx
    cmp     eax, 0
    jne     .convert_loop

    ; add sign if needed
    cmp     edx, 1          ; sign flag stored in edx earlier
    jne     .reverse
    mov     byte [ebx + ecx], '-'
    inc     ecx

.reverse:
    ; reverse digits in-place
    mov     esi, ebx
    mov     edi, ebx
    add     edi, ecx
    dec     edi             ; last char index

.rev_loop:
    cmp     esi, edi
    jge     .build_done
    mov     al, [esi]
    mov     ah, [edi]
    mov     [esi], ah
    mov     [edi], al
    inc     esi
    dec     edi
    jmp     .rev_loop

.build_done:
    ; write digits
    mov     edx, ecx
    mov     ecx, ebx
    mov     ebx, 1
    mov     eax, 4
    int     0x80

    ; restore stack
    add     esp, 16
    pop     ebp
    ret

; ------------------------------------------------------------
; print_space: prints single space
; ------------------------------------------------------------
print_space:
    mov     eax, ' '
    push    eax
    mov     ecx, esp
    mov     edx, 1
    mov     ebx, 1
    mov     eax, 4
    int     0x80
    pop     eax
    ret

; ------------------------------------------------------------
; parse_ints:
;   input_buf contains ASCII line
;   fills int_array, sets int_count
; ------------------------------------------------------------
parse_ints:
    mov     esi, input_buf
    mov     edi, int_array
    xor     eax, eax        ; current value
    xor     ebx, ebx        ; sign: 0=+,1=-
    mov     dword [int_count], 0

.next_char:
    mov     dl, [esi]
    cmp     dl, 0
    je      .end_line

    cmp     dl, ' '
    je      .maybe_end_num

    cmp     dl, 10          ; newline
    je      .maybe_end_num

    cmp     dl, '-'
    jne     .digit
    mov     ebx, 1          ; sign
    inc     esi
    jmp     .next_char

.digit:
    cmp     dl, '0'
    jb      .skip_char
    cmp     dl, '9'
    ja      .skip_char

    sub     dl, '0'
    mov     ecx, 10
    imul    eax, ecx
    add     eax, edx
    inc     esi
    jmp     .next_char

.skip_char:
    inc     esi
    jmp     .next_char

.maybe_end_num:
    ; if we have a number in progress (eax != 0 or sign set),
    ; store it
    cmp     eax, 0
    jne     .store_num
    cmp     ebx, 0
    jne     .store_num
    ; no number, just skip
    inc     esi
    jmp     .next_char

.store_num:
    cmp     ebx, 0
    je      .no_neg
    neg     eax
.no_neg:
    mov     [edi], eax
    add     edi, 4
    ; increment count
    mov     ecx, [int_count]
    inc     ecx
    mov     [int_count], ecx

    ; reset for next
    xor     eax, eax
    xor     ebx, ebx

    inc     esi
    jmp     .next_char

.end_line:
    ; also store last number if any
    cmp     eax, 0
    jne     .store_last
    cmp     ebx, 0
    jne     .store_last
    ret

.store_last:
    cmp     ebx, 0
    je      .no_neg2
    neg     eax
.no_neg2:
    mov     [edi], eax
    add     edi, 4
    mov     ecx, [int_count]
    inc     ecx
    mov     [int_count], ecx
    ret

; ------------------------------------------------------------
; quicksort(int *arr, int left, int right)
;   arr in esi, left in eax, right in ebx
; ------------------------------------------------------------
quicksort:
    push    ebp
    mov     ebp, esp
    push    esi
    push    eax
    push    ebx
    push    edx
    push    ecx

    ; if left >= right, return
    cmp     eax, ebx
    jge     .qs_done

    ; pivot = arr[(left+right)/2]
    mov     ecx, eax
    add     ecx, ebx
    shr     ecx, 1
    mov     edx, [esi + ecx*4]

    mov     edi, eax        ; i = left
    mov     ecx, ebx        ; j = right

.qs_loop:
    ; while arr[i] < pivot: i++
.qs_i_loop:
    mov     eax, [esi + edi*4]
    cmp     eax, edx
    jl      .qs_i_inc
    jmp     .qs_i_done
.qs_i_inc:
    inc     edi
    jmp     .qs_i_loop
.qs_i_done:

    ; while arr[j] > pivot: j--
.qs_j_loop:
    mov     eax, [esi + ecx*4]
    cmp     eax, edx
    jg      .qs_j_dec
    jmp     .qs_j_done
.qs_j_dec:
    dec     ecx
    jmp     .qs_j_loop
.qs_j_done:

    cmp     edi, ecx
    jg      .qs_partition_done

    ; swap arr[i], arr[j]
    mov     eax, [esi + edi*4]
    mov     ebx, [esi + ecx*4]
    mov     [esi + edi*4], ebx
    mov     [esi + ecx*4], eax

    inc     edi
    dec     ecx
    jmp     .qs_loop

.qs_partition_done:
    ; recurse: quicksort(arr, left, j)
    mov     eax, [ebp+8]    ; left
    mov     ebx, ecx        ; j
    push    ebx
    push    eax
    push    esi
    call    quicksort
    add     esp, 12

    ; recurse: quicksort(arr, i, right)
    mov     eax, edi        ; i
    mov     ebx, [ebp+12]   ; right
    push    ebx
    push    eax
    push    esi
    call    quicksort
    add     esp, 12

.qs_done:
    pop     ecx
    pop     edx
    pop     ebx
    pop     eax
    pop     esi
    pop     ebp
    ret

; ------------------------------------------------------------
; main
; ------------------------------------------------------------
main:
    ; print prompt
    mov     ecx, prompt_msg
    mov     edx, prompt_len
    mov     ebx, 1
    mov     eax, 4
    int     0x80

    ; read line
    mov     ebx, 0          ; stdin
    mov     ecx, input_buf
    mov     edx, buf_size
    call    read

    ; zero-terminate
    mov     ebx, eax        ; bytes read
    mov     byte [input_buf + ebx], 0

    ; parse integers
    call    parse_ints

    ; if no ints, exit
    mov     eax, [int_count]
    cmp     eax, 0
    je      .exit

    ; quicksort
    mov     esi, int_array
    mov     eax, 0                  ; left index
    mov     ebx, [int_count]
    dec     ebx                     ; right index
    call    quicksort

    ; print "Sorted: "
    mov     ecx, out_prefix
    call    print_cstring

    ; print all ints
    mov     ecx, [int_count]
    mov     esi, int_array
.print_loop:
    cmp     ecx, 0
    je      .print_done

    mov     eax, [esi]
    call    print_int
    call    print_space

    add     esi, 4
    dec     ecx
    jmp     .print_loop

.print_done:
    ; newline
    mov     ecx, nl
    call    print_cstring

.exit:
    mov     eax, 1
    xor     ebx, ebx
    int     0x80
