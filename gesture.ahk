#Requires AutoHotkey v2.0

~RButton::
{
    MouseGetPos(&x1, &y1)

    KeyWait("RButton")

    MouseGetPos(&x2, &y2)

    dx := x2 - x1
    dy := y2 - y1

    if (Abs(dx) > Abs(dy))
    {
        if (dx > 100)
            MsgBox("→")
        else if (dx < -100)
            MsgBox("←")
    }
}

#Requires AutoHotkey v2.0

lastCtrlTime := 0

~Ctrl::
{
    global lastCtrlTime

    currentTime := A_TickCount

    if (currentTime - lastCtrlTime < 300)
    {
        MsgBox("Double Ctrl")
    }

    lastCtrlTime := currentTime
}