   M S B A M L   `   `   `  ����G  CSpaceShooter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null+clr-namespace:SpaceShooterSpaceShooter  5          SpaceShooter.Settings   5      W SPresentationCore, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      ,localclr-namespace:SpaceShooter     5      $
|�True��5      .��5
      � ��5   
   �� 5        �� ImageSource$6  0pack://application:,,,/images/menubackground.png��6    j� Stretch$ UniformToFill=�6X   6r    � 	Resources 5   
    �� 
TargetType �� Property �� Value �� Triggers P� Property P� Value��%
  '��       �� 5      " ��6   .�5      ��  � ��$ 40��6+   68   �� 5       � ��$	 130��6*   68   �� 5       � ��$ 20(�6-   6:   �� 5       � ��$ Light"�6/   6?   �� 5       � ��$
 Nugo)�6/   6>   �� 5       � ��
 �����6?   �� 5       � ��
 ���� 6E   �� 5       � ��
 ���� 6F    5      P� 5       �@J�IsMouseOver$
 True��65   . �5      ��  � ��
 ����6L   �� 5       � ��
 ���� 6M   5      5       5!      .+�5$   
   �� $	��Top=�6   $
��Left=�6-   $	��280��6H   $	��130��6U   $��100,100,0,0q�6a   .+�5%      �� -   5        $��Up��5%      
	 � Tag$	 	MoveUpKey��6R   �� 5&      -   5        $
��Down��5&      $	 MoveDownKey��6V   �� 5'      -   5        $
��Left��5'      $	 MoveLeftKey��6V   �� 5(      -   5        $��Right��5(      $	 MoveRightKey��6X   �� 5)      -   5        $
��Fire��5)      $	 FireKey��6V   �� 5*      -   5        $��Mouse Control��5*      $	 MouseToggleButton��6g   �� 5+      -   5        $��Reset��5+      $	 ResetButton��6Z   5,      �� 5.   
   -   5        $��text_block_settings��5.      $ ��Settings - change keybinds��6/   
�������$��Medium"�6d   $��40(�6x   $	��Top=�6�   $
��Left=�6�   $��	30,30,0,0q�6�   6�   �� 5/   
   -	   5        $
��Exit��5/      $��0,390,675,0q�65   �� 50   
   -
   5        $��prompt��50      
�������$��16(�61   $��DemiBold"�6?   $
��Left=�6U   
 �� Left$
 10��6p   
 �� Top$ 10��6�   $	��Top=�6�   $��346,114,0,0q�6�   6�   51      52      