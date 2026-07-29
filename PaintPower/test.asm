section .data
    msg db "Hello, World"
    len equ $ - msg

section .text
    global _start

_start:
    ; sys_write (stdout, msg, len)
    mov rax, 1          ; syscall number for write
    mov rdi, 1          ; file descriptor 1 = stdout
    mov rsi, msg        ; pointer to message
    mov rdx, len        ; message length
    syscall             ; invoke kernel

    ; sys_exit (0)
    mov rax, 60         ; syscall number for exit
    xor rdi, rdi        ; exit code 0
    syscall