<?php

function syscall($code, $arg = null) {
    if ($code == 0) {
       return readline();
    } else if ($code == 1) {
       print($arg); 
    }
   
    return 0;
}